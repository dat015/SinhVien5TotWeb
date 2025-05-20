using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SinhVien5TotWeb.Data;
using SinhVien5TotWeb.Models;
using System.Security.Claims;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using SinhVien5TotWeb.Services;

namespace SinhVien5TotWeb.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<StudentController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationService _applicationService;

        public StudentController(AppDbContext context, ILogger<StudentController> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _applicationService = new ApplicationService(_context);
        }

        public async Task<IActionResult> Dashboard()
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var student = await _context.Students
                .Include(s => s.Applications)
                    .ThenInclude(a => a.Scores)
                .Include(s => s.Applications)
                    .ThenInclude(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
            {
                return NotFound();
            }

            ViewBag.TotalApplications = student.Applications.Count;
            ViewBag.PendingApplications = student.Applications.Count(a => a.Status == Models.ApplicationStatus.Pending);
            ViewBag.ApprovedApplications = student.Applications.Count(a => a.Status == Models.ApplicationStatus.Approved);
            ViewBag.RejectedApplications = student.Applications.Count(a => a.Status == Models.ApplicationStatus.Rejected);

            ViewBag.RecentApplications = student.Applications
                .OrderByDescending(a => a.SubmissionDate)
                .Take(5)
                .ToList();

            ViewBag.Notifications = await _context.Notifications
                .Where(n => n.StudentId == studentId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View();
        }

        public IActionResult Profile()
        {
            return View();
        }

        public async Task<IActionResult> Application()
        {
            var criteria = await _context.Criteria
                .Where(c => c.IsActive)
                .ToListAsync();

            var model = new Application
            {
                ApplicationCriteria = new List<ApplicationCriterion>()
            };
            ViewBag.Criteria = criteria; // Truyền danh sách tiêu chí để hiển thị trong view
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitApplication(IFormCollection form)
        {
            try
            {
                var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
                var student = await _context.Students.FindAsync(studentId);

                if (student == null)
                {
                    return NotFound();
                }

                var submissionDate = DateTime.UtcNow;
                var academicYear = _applicationService.GetAcademicYear(submissionDate);

                var existingApplication = await _context.Applications
                    .FirstOrDefaultAsync(a => a.StudentId == studentId && a.AcademicYear == academicYear);

                if (existingApplication != null)
                {
                    ModelState.AddModelError("", "Bạn đã đăng ký danh hiệu Sinh viên 5 Tốt trong năm học này.");
                    var criteria = await _context.Criteria.Where(c => c.IsActive).ToListAsync();
                    var model = new Application { ApplicationCriteria = new List<ApplicationCriterion>() };
                    ViewBag.Criteria = criteria;
                    return View("Application", model);
                }

                var application = new Application
                {
                    StudentId = studentId,
                    CurrentLevel = ApplicationLevel.Faculty,
                    Status = Models.ApplicationStatus.Pending,
                    SubmissionDate = submissionDate,
                    AcademicYear = academicYear,
                    CreatedAt = DateTime.UtcNow,
                    ApplicationCriteria = new List<ApplicationCriterion>()
                };

                var selectedCriteriaIds = form["criteria"].ToString().Split(',')
                    .Where(id => !string.IsNullOrEmpty(id))
                    .Select(int.Parse)
                    .ToList();

                if (selectedCriteriaIds.Any())
                {
                    var selectedCriteria = await _context.Criteria
                        .Where(c => selectedCriteriaIds.Contains(c.Id))
                        .ToListAsync();

                    foreach (var criterion in selectedCriteria)
                    {
                        application.ApplicationCriteria.Add(new ApplicationCriterion
                        {
                            Application = application,
                            Criterion = criterion,
                            AssignedAt = DateTime.UtcNow
                        });
                    }
                }

                var isCreated = await _applicationService.CreateApplicationAsync(application);
                if (!isCreated)
                {
                    _logger.LogError("Lỗi khi nộp hồ sơ");
                    ModelState.AddModelError("", "Có lỗi xảy ra khi nộp hồ sơ. Vui lòng thử lại sau.");
                    var criteria = await _context.Criteria.Where(c => c.IsActive).ToListAsync();
                    var model = new Application { ApplicationCriteria = new List<ApplicationCriterion>() };
                    ViewBag.Criteria = criteria;
                    return View("Application", model);
                }

                foreach (var criterionId in selectedCriteriaIds)
                {
                    var files = form.Files.GetFiles($"evidences_{criterionId}");
                    foreach (var file in files)
                    {
                        if (file != null && file.Length > 0)
                        {
                            var uploadsDir = Path.Combine(_environment.WebRootPath, "Uploads", "evidences");
                            if (!Directory.Exists(uploadsDir))
                            {
                                Directory.CreateDirectory(uploadsDir);
                            }

                            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            var evidence = new Evidence
                            {
                                Application = application,
                                Criterion = await _context.Criteria.FindAsync(criterionId),
                                Title = file.FileName,
                                FilePath = $"/Uploads/evidences/{fileName}",
                                Description = $"Minh chứng cho tiêu chí {criterionId}",
                                CreatedAt = DateTime.UtcNow
                            };

                            await _context.Evidences.AddAsync(evidence);
                        }
                    }
                }

                var scorers = await _context.Officers
                    .Where(o => o.Level == OfficerLevel.Faculty && o.Role == OfficerRole.Scorer && o.IsActive)
                    .Take(3)
                    .ToListAsync();

                if (scorers.Count < 2)
                {
                    _logger.LogWarning("Không đủ cán bộ chấm điểm cho cấp Khoa.");
                    ModelState.AddModelError("", "Hệ thống hiện không có đủ cán bộ chấm điểm. Vui lòng thử lại sau.");
                    var criteria = await _context.Criteria.Where(c => c.IsActive).ToListAsync();
                    var model = new Application { ApplicationCriteria = new List<ApplicationCriterion>() };
                    ViewBag.Criteria = criteria;
                    return View("Application", model);
                }

                foreach (var scorer in scorers)
                {
                    foreach (var appCriterion in application.ApplicationCriteria)
                    {
                        var scoringResult = new ScoringResult
                        {
                            Application = application,
                            Criterion = appCriterion.Criterion,
                            Officer = scorer,
                            Score = 0,
                            Comment = "Chưa chấm",
                            ScoredAt = DateTime.UtcNow
                        };
                        await _context.ScoringResults.AddAsync(scoringResult);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Hồ sơ đã được nộp thành công!";
                return RedirectToAction(nameof(ApplicationStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nộp hồ sơ");
                ModelState.AddModelError("", "Có lỗi xảy ra khi nộp hồ sơ. Vui lòng thử lại sau.");
                var criteria = await _context.Criteria.Where(c => c.IsActive).ToListAsync();
                var model = new Application { ApplicationCriteria = new List<ApplicationCriterion>() };
                ViewBag.Criteria = criteria;
                return View("Application", model);
            }
        }

        public async Task<IActionResult> ApplicationStatus()
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var applications = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Include(a => a.Evidences)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            return View(applications);
        }

        public IActionResult Evidence()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadEvidence(Evidence model)
        {
            if (!ModelState.IsValid)
            {
                return View("Evidence", model);
            }

            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            await _context.Evidences.AddAsync(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("Evidence");
        }

        public async Task<IActionResult> History()
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var history = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Where(a => a.StudentId == studentId)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            return View(history);
        }

        public async Task<IActionResult> ViewApplication(int id)
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var application = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion.ScoringResults)
                    .ThenInclude(sr => sr.Officer)
                .Include(a => a.Evidences)
                .Include(a => a.Scores)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }
        [HttpGet("/Student/SupplementApplication/{id}")]
        public async Task<IActionResult> SupplementApplication(int id)
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out var studentId))
            {
                _logger.LogWarning("Invalid or missing StudentId claim for user.");
                TempData["ErrorMessage"] = "Không thể xác định thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            var application = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Include(a => a.Evidences)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null)
            {
                _logger.LogWarning("Application ID: {ApplicationId} not found or not owned by student ID: {StudentId}", id, studentId);
                TempData["ErrorMessage"] = "Hồ sơ không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            if (application.Status != Models.ApplicationStatus.SupplementRequested && application.Status != Models.ApplicationStatus.Rejected)
            {
                _logger.LogWarning("Application ID: {ApplicationId} is not in SupplementRequested status. Current status: {Status}", id, application.Status);
                TempData["ErrorMessage"] = "Hồ sơ không ở trạng thái cần bổ sung minh chứng.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            return View(application);
        }

        [HttpPost("/Student/SupplementApplication")]
        public async Task<IActionResult> SubmitSupplement(int id, Dictionary<int, EvidenceInput> evidences)
        {
            // Kiểm tra StudentId từ claims
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out var studentId))
            {
                _logger.LogWarning("Invalid or missing StudentId claim for user.");
                TempData["ErrorMessage"] = "Không thể xác định thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            // Tải hồ sơ với dữ liệu liên quan
            var application = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Include(a => a.Evidences)
                    .ThenInclude(e => e.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null)
            {
                _logger.LogWarning("Application ID: {ApplicationId} not found or not owned by student ID: {StudentId}", id, studentId);
                TempData["ErrorMessage"] = "Hồ sơ không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            // Kiểm tra trạng thái hồ sơ
            if (application.Status != Models.ApplicationStatus.SupplementRequested && application.Status != Models.ApplicationStatus.Rejected)
            {
                _logger.LogWarning("Application ID: {ApplicationId} is not in SupplementRequested or Rejected status. Current status: {Status}", id, application.Status);
                TempData["ErrorMessage"] = $"Hồ sơ hiện đang ở trạng thái {application.Status}. Chỉ hồ sơ ở trạng thái 'Yêu cầu chỉnh sửa minh chứng' hoặc 'Bị từ chối' mới có thể chỉnh sửa.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            try
            {
                // Kiểm tra tệp đầu vào
                if (evidences == null || !evidences.Any(e => e.Value.File.Any(f => f != null && f.Length > 0)))
                {
                    _logger.LogWarning("No valid evidence files provided for application ID: {ApplicationId}", id);
                    TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một tệp minh chứng hợp lệ để tải lên.";
                    return View("SupplementApplication", application);
                }

                bool hasValidFiles = false;
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    foreach (var evidenceGroup in evidences)
                    {
                        var criterionId = evidenceGroup.Key;
                        var input = evidenceGroup.Value;

                        // Kiểm tra CriterionId
                        var criterion = await _context.Criteria.FindAsync(criterionId);
                        if (criterion == null)
                        {
                            _logger.LogWarning("Invalid CriterionId {CriterionId} for application ID: {ApplicationId}", criterionId, id);
                            TempData["ErrorMessage"] = $"Tiêu chí {criterionId} không tồn tại.";
                            return View("SupplementApplication", application);
                        }

                        // Kiểm tra tiêu chí thuộc hồ sơ
                        if (!application.ApplicationCriteria.Any(ac => ac.CriterionId == criterionId))
                        {
                            _logger.LogWarning("CriterionId {CriterionId} does not belong to application ID: {ApplicationId}", criterionId, id);
                            TempData["ErrorMessage"] = $"Tiêu chí {criterionId} không thuộc hồ sơ này.";
                            return View("SupplementApplication", application);
                        }

                        var uploadsDir = Path.Combine(_environment.WebRootPath, "Uploads", "evidences");
                        try
                        {
                            if (!Directory.Exists(uploadsDir))
                            {
                                Directory.CreateDirectory(uploadsDir);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create directory {UploadsDir} for application ID: {ApplicationId}", uploadsDir, id);
                            TempData["ErrorMessage"] = "Lỗi khi tạo thư mục lưu trữ. Vui lòng liên hệ quản trị viên.";
                            return View("SupplementApplication", application);
                        }

                        // Xử lý các tệp tải lên
                        foreach (var file in input.File.Where(f => f != null && f.Length > 0))
                        {
                            // Kiểm tra kích thước tệp (5MB) và định dạng
                            if (file.Length > 5 * 1024 * 1024)
                            {
                                _logger.LogWarning("File {FileName} exceeds 5MB limit for application ID: {ApplicationId}", file.FileName, id);
                                TempData["ErrorMessage"] = $"Tệp {file.FileName} vượt quá kích thước cho phép (5MB).";
                                return View("SupplementApplication", application);
                            }

                            // Thêm application/vnd.openxmlformats-officedocument.wordprocessingml.document cho .docx
                            if (!new[] { "image/jpeg", "image/png", "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" }.Contains(file.ContentType))
                            {
                                _logger.LogWarning("Invalid file type {FileType} for file {FileName} in application ID: {ApplicationId}", file.ContentType, file.FileName, id);
                                TempData["ErrorMessage"] = $"Tệp {file.FileName} không đúng định dạng (chỉ chấp nhận JPEG, PNG, PDF, DOCX).";
                                return View("SupplementApplication", application);
                            }

                            var fileExtension = Path.GetExtension(file.FileName);
                            var fileName = $"{Guid.NewGuid()}{fileExtension}";
                            var filePath = Path.Combine(uploadsDir, fileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            // Nếu có EvidenceId, chỉnh sửa minh chứng hiện có
                            if (input.EvidenceId.HasValue)
                            {
                                var existingEvidence = await _context.Evidences
                                    .FirstOrDefaultAsync(e => e.Id == input.EvidenceId);

                                if (existingEvidence == null)
                                {
                                    _logger.LogWarning("Evidence ID {EvidenceId} not found for application ID: {ApplicationId}, criterion ID: {CriterionId}", input.EvidenceId, id, criterionId);
                                    TempData["ErrorMessage"] = $"Minh chứng ID {input.EvidenceId} không tồn tại.";
                                    return View("SupplementApplication", application);
                                }

                                // Xóa tệp cũ nếu tồn tại
                                if (!string.IsNullOrEmpty(existingEvidence.FilePath))
                                {
                                    var oldFilePath = Path.Combine(_environment.WebRootPath, existingEvidence.FilePath.TrimStart('/'));
                                    if (System.IO.File.Exists(oldFilePath))
                                    {
                                        System.IO.File.Delete(oldFilePath);
                                    }
                                }

                                // Cập nhật minh chứng
                                existingEvidence.Title = Path.GetFileName(file.FileName);
                                existingEvidence.FilePath = $"/Uploads/evidences/{fileName}";
                                existingEvidence.Description = $"Minh chứng chỉnh sửa cho tiêu chí {criterion.Name}";
                                existingEvidence.UpdatedAt = DateTime.UtcNow;

                                _context.Evidences.Update(existingEvidence);
                            }
                            // Nếu không có EvidenceId, thêm minh chứng mới
                            else
                            {
                                var evidence = new Evidence
                                {
                                    Application = application,
                                    Criterion = criterion,
                                    Title = Path.GetFileName(file.FileName),
                                    FilePath = $"/Uploads/evidences/{fileName}",
                                    Description = $"Minh chứng chỉnh sửa cho tiêu chí {criterion.Name}",
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _context.Evidences.AddAsync(evidence);
                            }

                            hasValidFiles = true;
                        }
                    }

                    if (!hasValidFiles)
                    {
                        _logger.LogWarning("No valid evidence files provided for application ID: {ApplicationId}", id);
                        TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một tệp minh chứng hợp lệ (JPEG, PNG, PDF, DOCX).";
                        return View("SupplementApplication", application);
                    }

                    application.Status = Models.ApplicationStatus.Pending;
                    application.LastUpdated = DateTime.UtcNow;
                    application.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }

                TempData["SuccessMessage"] = "Minh chứng đã chỉnh sửa đã được nộp thành công!";
                return RedirectToAction(nameof(ApplicationStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting edited evidence for application ID: {ApplicationId}, student ID: {StudentId}", id, studentId);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi nộp minh chứng đã chỉnh sửa. Vui lòng thử lại sau.";
                return View("SupplementApplication", application);
            }
        }
        [HttpGet("/Student/DownloadEvidence/{evidenceId}")]
        public async Task<IActionResult> DownloadEvidence(int evidenceId)
        {
            // Kiểm tra StudentId từ claims
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out var studentId))
            {
                _logger.LogWarning("Invalid or missing StudentId claim for user.");
                TempData["ErrorMessage"] = "Không thể xác định thông tin sinh viên. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            // Tìm minh chứng
            var evidence = await _context.Evidences
                .Include(e => e.Application)
                .FirstOrDefaultAsync(e => e.Id == evidenceId && e.Application.StudentId == studentId);
            if (evidence == null)
            {
                _logger.LogWarning("Evidence ID: {EvidenceId} not found or not owned by student ID: {StudentId}", evidenceId, studentId);
                TempData["ErrorMessage"] = "Minh chứng không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            // Kiểm tra tệp
            var filePath = Path.Combine(_environment.WebRootPath, evidence.FilePath.TrimStart('/'));
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found at path: {FilePath} for evidence ID: {EvidenceId}", filePath, evidenceId);
                TempData["ErrorMessage"] = "Tệp minh chứng không tồn tại.";
                return RedirectToAction(nameof(ApplicationStatus));
            }

            // Đọc tệp và trả về
            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(filePath);

            return File(fileBytes, contentType, Path.GetFileName(filePath));
        }

        // Hàm phụ để lấy contentType
        private string GetContentType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
    // Record để ánh xạ dữ liệu từ form
    public record EvidenceInput
    {
        public int? EvidenceId { get; init; } // ID của minh chứng hiện có để chỉnh sửa
        public List<IFormFile> File { get; init; } = new List<IFormFile>(); // Danh sách tệp tải lên
    }
}
