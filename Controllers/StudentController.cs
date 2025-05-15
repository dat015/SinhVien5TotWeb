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
                .Include(a => a.Evidences)
                .Include(a => a.Scores)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null)
            {
                return NotFound();
            }

            return View(application);
        }

        public async Task<IActionResult> SupplementApplication(int id)
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var application = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .Include(a => a.Evidences)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null || application.Status != Models.ApplicationStatus.SupplementRequested)
            {
                return NotFound();
            }

            return View(application);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitSupplement(int id, Dictionary<int, List<IFormFile>> evidences)
        {
            var studentId = int.Parse(User.FindFirst("StudentId")?.Value ?? "0");
            var application = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == id && a.StudentId == studentId);

            if (application == null || application.Status != Models.ApplicationStatus.SupplementRequested)
            {
                return NotFound();
            }

            try
            {
                if (evidences != null)
                {
                    foreach (var evidenceGroup in evidences)
                    {
                        var criterionId = evidenceGroup.Key;
                        var files = evidenceGroup.Value;

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
                                    Description = $"Minh chứng bổ sung cho tiêu chí {criterionId}",
                                    CreatedAt = DateTime.UtcNow
                                };

                                await _context.Evidences.AddAsync(evidence);
                            }
                        }
                    }

                    application.Status = Models.ApplicationStatus.Pending;
                    application.LastUpdated = DateTime.UtcNow;
                    application.UpdatedAt = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }

                TempData["SuccessMessage"] = "Minh chứng bổ sung đã được nộp thành công!";
                return RedirectToAction(nameof(ApplicationStatus));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nộp minh chứng bổ sung");
                ModelState.AddModelError("", "Có lỗi xảy ra khi nộp minh chứng bổ sung. Vui lòng thử lại sau.");
                return View("SupplementApplication", application);
            }
        }
    }
}