using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SinhVien5TotWeb.Models;
using System.Threading.Tasks;
using SinhVien5TotWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.ComponentModel.DataAnnotations;

namespace SinhVien5TotWeb.Controllers
{
    [Authorize(Roles = "Officer")]
    public class OfficerController : Controller
    {
        private readonly ILogger<OfficerController> _logger;
        private readonly AppDbContext _context;

        public OfficerController(ILogger<OfficerController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        private ApplicationLevel MapOfficerLevelToApplicationLevel(OfficerLevel officerLevel)
        {
            return officerLevel switch
            {
                OfficerLevel.Faculty => ApplicationLevel.Faculty,
                OfficerLevel.University => ApplicationLevel.University,
                OfficerLevel.Province => ApplicationLevel.Province,
                OfficerLevel.National => ApplicationLevel.National,
                _ => throw new ArgumentException($"Cấp cán bộ không hợp lệ: {officerLevel}")
            };
        }

        public async Task<IActionResult> Dashboard()
        {
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers
                .FirstOrDefaultAsync(o => o.Id == officerId);

            if (officer == null)
            {
                return NotFound();
            }

            var officerApplicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var applicationsQuery = _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Scores)
                .ThenInclude(s => s.Criterion)
                .Where(a => a.CurrentLevel == officerApplicationLevel);

            if (officer.Role == OfficerRole.Scorer)
            {
                applicationsQuery = applicationsQuery
                    .Join(_context.ScoringResults,
                          a => a.ApplicationId,
                          s => s.Application.ApplicationId,
                          (a, s) => new { Application = a, ScoringResult = s })
                    .Where(x => x.ScoringResult.Officer.Id == officerId)
                    .Select(x => x.Application)
                    .Distinct();
            }
            else if (officer.Role == OfficerRole.Approver && officerApplicationLevel != ApplicationLevel.Faculty)
            {
                var requiredScores = 2; // 2 scores for University, Province, National
                applicationsQuery = applicationsQuery
                    .Where(a => a.Scores.Count(s => s.Score > 0) >= requiredScores);
            }
            // For Faculty Approver, show all applications (no score filtering)

            var applications = await applicationsQuery.ToListAsync();

            ViewBag.TotalApplications = applications.Count;
            ViewBag.PendingScoring = officer.Role == OfficerRole.Scorer
                ? applications.Count(a => a.Status == ApplicationStatus.Pending &&
                    _context.ScoringResults.Any(s => s.Application.ApplicationId == a.ApplicationId &&
                                                    s.Officer.Id == officerId && s.Score == 0))
                : 0;
            ViewBag.PendingApproval = officer.Role == OfficerRole.Approver
                ? applications.Count(a => a.Status == ApplicationStatus.Pending &&
                    (officerApplicationLevel == ApplicationLevel.Faculty
                        ? a.Scores.Count(s => s.Score > 0) >= 3
                        : a.Scores.Count(s => s.Score > 0) >= 2))
                : 0;
            ViewBag.ProcessedApplications = applications.Count(a => a.Status == ApplicationStatus.Approved || a.Status == ApplicationStatus.Rejected);

            ViewBag.PendingApplications = applications
                .Where(a => a.Status == ApplicationStatus.Pending)
                .OrderByDescending(a => a.SubmissionDate)
                .Take(5)
                .ToList();

            var facultyStats = applications
                .GroupBy(a => a.Student.Faculty)
                .Select(g => new { Faculty = g.Key ?? "Không xác định", Count = g.Count() })
                .ToList();

            ViewBag.FacultyLabels = facultyStats.Select(s => s.Faculty).ToList();
            ViewBag.FacultyData = facultyStats.Select(s => s.Count).ToList();

            var statusStats = applications
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            ViewBag.StatusLabels = statusStats.Select(s => s.Status.ToString()).ToList();
            ViewBag.StatusData = statusStats.Select(s => s.Count).ToList();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForwardApplication(int applicationId, string nextLevel)
        {
            var application = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .Include(a => a.Scores)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                return NotFound();
            }

            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers.FindAsync(officerId);

            if (officer == null || officer.Role != OfficerRole.Approver || MapOfficerLevelToApplicationLevel(officer.Level) != application.CurrentLevel)
            {
                return Unauthorized("Bạn không có quyền chuyển hồ sơ này.");
            }

            if (application.Status != ApplicationStatus.Approved)
            {
                TempData["ErrorMessage"] = "Hồ sơ chưa được duyệt tại cấp hiện tại.";
                return RedirectToAction("Workflow");
            }

            if (!Enum.TryParse<ApplicationLevel>(nextLevel, out var newLevel) ||
                (int)newLevel != (int)application.CurrentLevel + 1)
            {
                TempData["ErrorMessage"] = "Cấp chuyển tiếp không hợp lệ.";
                return RedirectToAction("Workflow");
            }

            application.CurrentLevel = newLevel;
            application.Status = ApplicationStatus.Pending;
            application.LastUpdated = DateTime.UtcNow;
            application.UpdatedAt = DateTime.UtcNow;

            var scorerCount = newLevel == ApplicationLevel.Faculty ? 3 : 2;
            var nextLevelScorers = await _context.Officers
                .Where(o => MapOfficerLevelToApplicationLevel(o.Level) == newLevel && o.Role == OfficerRole.Scorer && o.IsActive)
                .Take(scorerCount)
                .ToListAsync();

            if (nextLevelScorers.Count < scorerCount)
            {
                _logger.LogWarning($"Không đủ cán bộ chấm điểm cho cấp {newLevel}.");
                TempData["ErrorMessage"] = $"Không đủ cán bộ chấm điểm cho cấp {newLevel}.";
                return RedirectToAction("Workflow");
            }

            var oldScores = await _context.ScoringResults
                .Where(s => s.Application.ApplicationId == applicationId)
                .ToListAsync();
            _context.ScoringResults.RemoveRange(oldScores);

            foreach (var scorer in nextLevelScorers)
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

            var notification = new Notification
            {
                StudentId = application.StudentId,
                Title = "Hồ sơ được chuyển cấp",
                Message = $"Hồ sơ của bạn đã được chuyển lên cấp {newLevel} để xét duyệt.",
                Type = "info",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _context.Notifications.AddAsync(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Hồ sơ đã được chuyển lên cấp {newLevel} thành công!";
            return RedirectToAction("Workflow");
        }

        public async Task<IActionResult> Applications()
        {
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers
                .FirstOrDefaultAsync(o => o.Id == officerId);

            if (officer == null)
            {
                return NotFound();
            }

            var officerApplicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var applicationsQuery = _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Scores)
                .ThenInclude(s => s.Criterion)
                .Include(a => a.Evidences)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .Where(a => a.CurrentLevel == officerApplicationLevel);

            ViewBag.OfficerRole = officer.Role;
            ViewBag.OfficerLevel = officerApplicationLevel;

            if (officer.Role == OfficerRole.Scorer)
            {
                applicationsQuery = applicationsQuery
                    .Join(_context.ScoringResults,
                          a => a.ApplicationId,
                          s => s.Application.ApplicationId,
                          (a, s) => new { Application = a, ScoringResult = s })
                    .Where(x => x.ScoringResult.Officer.Id == officerId &&
                                x.ScoringResult.Score == 0 &&
                                x.Application.Status == ApplicationStatus.Pending)
                    .Select(x => x.Application)
                    .Distinct();
            }
            else if (officer.Role == OfficerRole.Approver)
            {
                applicationsQuery = applicationsQuery
                    .Where(a => a.Status == ApplicationStatus.Pending &&
                        (officerApplicationLevel == ApplicationLevel.Faculty
                            ? a.Scores.Count(s => s.Score > 0) >= 3
                            : a.Scores.Count(s => s.Score > 0) >= 2));
            }

            var applications = await applicationsQuery
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            // Cho Approver cấp khoa xem tất cả hồ sơ
            if (officer.Role == OfficerRole.Approver && officerApplicationLevel == ApplicationLevel.Faculty)
            {
                var allApplicationsQuery = _context.Applications
                    .Include(a => a.Student)
                    .Include(a => a.Scores)
                    .ThenInclude(s => s.Criterion)
                    .Include(a => a.Evidences)
                    .Include(a => a.ApplicationCriteria)
                    .ThenInclude(ac => ac.Criterion)
                    .Where(a => a.CurrentLevel == officerApplicationLevel);
                ViewBag.AllApplications = await allApplicationsQuery
                    .OrderByDescending(a => a.SubmissionDate)
                    .ToListAsync();
            }

            return View(applications);
        }
        [HttpPost]
        public async Task<IActionResult> ScoreApplication([FromQuery] int applicationId, [FromBody] List<ScoreInput> scores)
        {
            _logger.LogInformation("Gọi ScoreApplication cho ApplicationId: {ApplicationId}", applicationId);

            // Validate applicationId
            if (applicationId <= 0)
            {
                _logger.LogWarning("applicationId không hợp lệ: {ApplicationId}", applicationId);
                return BadRequest(new { message = "ID hồ sơ không hợp lệ." });
            }

            // Validate scores
            if (scores == null || !scores.Any())
            {
                _logger.LogWarning("Danh sách điểm rỗng hoặc null cho ApplicationId: {ApplicationId}", applicationId);
                return BadRequest(new { message = "Danh sách điểm không được rỗng." });
            }

            // Kiểm tra CriterionId trùng lặp
            var criterionIds = scores.Select(s => s.CriterionId).ToList();
            if (criterionIds.Distinct().Count() != criterionIds.Count)
            {
                _logger.LogWarning("Danh sách điểm chứa CriterionId trùng lặp cho ApplicationId: {ApplicationId}", applicationId);
                return BadRequest(new { message = "Danh sách điểm chứa tiêu chí trùng lặp." });
            }

            // Kiểm tra officer
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            if (officerId <= 0)
            {
                _logger.LogWarning("OfficerId không hợp lệ: {OfficerId}", officerId);
                return Unauthorized(new { message = "Không xác định được thông tin cán bộ." });
            }

            var officer = await _context.Officers.FirstOrDefaultAsync(o => o.Id == officerId);
            if (officer == null)
            {
                _logger.LogWarning("Không tìm thấy cán bộ với OfficerId: {OfficerId}", officerId);
                return NotFound(new { message = "Không tìm thấy thông tin cán bộ." });
            }

            if (officer.Role != OfficerRole.Scorer)
            {
                _logger.LogWarning("Cán bộ không có quyền chấm điểm. OfficerId: {OfficerId}, Role: {Role}", officerId, officer.Role);
                return Unauthorized(new { message = "Bạn không có quyền chấm điểm hồ sơ này." });
            }

            // Kiểm tra application
            var applicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var application = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId && a.CurrentLevel == applicationLevel);
            if (application == null)
            {
                _logger.LogWarning("Không tìm thấy hồ sơ với ApplicationId: {ApplicationId} hoặc CurrentLevel không khớp: {ExpectedLevel}", applicationId, applicationLevel);
                return NotFound(new { message = "Không tìm thấy hồ sơ hoặc bạn không có quyền truy cập." });
            }

            // Kiểm tra RejectionCount
            if (application.RejectionCount >= 2)
            {
                _logger.LogWarning("Hồ sơ đã bị từ chối 2 lần cho ApplicationId: {ApplicationId}", applicationId);
                return BadRequest(new { message = "Hồ sơ đã bị từ chối 2 lần và không thể chấm điểm thêm." });
            }

            // Lấy danh sách tiêu chí hợp lệ
            var validCriteria = await _context.ApplicationCriterias
                .Where(ac => ac.ApplicationId == applicationId)
                .Include(ac => ac.Criterion)
                .ToDictionaryAsync(ac => ac.CriterionId, ac => ac.Criterion);

            // Kiểm tra số lượng tiêu chí
            if (scores.Count != validCriteria.Count)
            {
                _logger.LogWarning("Chưa chấm đủ tiêu chí cho ApplicationId: {ApplicationId}. Nhận: {Received}, Yêu cầu: {Required}",
                    applicationId, scores.Count, validCriteria.Count);
                return BadRequest(new { message = $"Phải chấm điểm cho tất cả {validCriteria.Count} tiêu chí được gán." });
            }

            // Lấy danh sách ScoringResults
            var scoringResults = await _context.ScoringResults
                .Include(s => s.Criterion)
                .Where(s => s.ApplicationId == applicationId &&
                           s.OfficerId == officerId &&
                           s.CurrentLevel == application.CurrentLevel)
                .ToDictionaryAsync(s => s.CriterionId, s => s);

            // Validate mỗi ScoreInput
            foreach (var scoreInput in scores)
            {
                // Kiểm tra CriterionId
                if (scoreInput.CriterionId <= 0)
                {
                    _logger.LogWarning("CriterionId không hợp lệ: {CriterionId} cho ApplicationId: {ApplicationId}", scoreInput.CriterionId, applicationId);
                    return BadRequest(new { message = $"ID tiêu chí {scoreInput.CriterionId} không hợp lệ." });
                }

                // Kiểm tra tiêu chí tồn tại và được gán
                if (!validCriteria.TryGetValue(scoreInput.CriterionId, out var criterion))
                {
                    _logger.LogWarning("Tiêu chí không được gán cho hồ sơ. CriterionId: {CriterionId}, ApplicationId: {ApplicationId}", scoreInput.CriterionId, applicationId);
                    return BadRequest(new { message = $"Tiêu chí {scoreInput.CriterionId} không được gán cho hồ sơ." });
                }

                // Kiểm tra phân công
                if (!scoringResults.TryGetValue(scoreInput.CriterionId, out var scoringResult))
                {
                    _logger.LogWarning("Không tìm thấy bản ghi chấm điểm cho ApplicationId: {ApplicationId}, CriterionId: {CriterionId}, OfficerId: {OfficerId}",
                        applicationId, scoreInput.CriterionId, officerId);
                    return NotFound(new { message = $"Không tìm thấy bản ghi chấm điểm cho tiêu chí {scoreInput.CriterionId}." });
                }

                // Kiểm tra Score
                if (double.IsNaN(scoreInput.Score) || double.IsInfinity(scoreInput.Score))
                {
                    _logger.LogWarning("Điểm không hợp lệ (NaN hoặc Infinity) cho CriterionId: {CriterionId}, ApplicationId: {ApplicationId}", scoreInput.CriterionId, applicationId);
                    return BadRequest(new { message = $"Điểm cho tiêu chí {criterion.Name} không hợp lệ." });
                }

                if (scoreInput.Score < 0 || scoreInput.Score > criterion.MaxScore)
                {
                    _logger.LogWarning("Điểm ngoài khoảng cho phép cho CriterionId: {CriterionId}. Score: {Score}, MaxScore: {MaxScore}",
                        scoreInput.CriterionId, scoreInput.Score, criterion.MaxScore);
                    return BadRequest(new { message = $"Điểm cho tiêu chí {criterion.Name} phải từ 0 đến {criterion.MaxScore}." });
                }

                // Kiểm tra Comment
                if (string.IsNullOrWhiteSpace(scoreInput.Comment))
                {
                    scoreInput.Comment = "";
                }
                else if (scoreInput.Comment.Length > 1000)
                {
                    _logger.LogWarning("Nhận xét quá dài cho CriterionId: {CriterionId}, ApplicationId: {ApplicationId}", scoreInput.CriterionId, applicationId);
                    return BadRequest(new { message = $"Nhận xét cho tiêu chí {criterion.Name} không được vượt quá 1000 ký tự." });
                }

                // Cập nhật ScoringResult
                scoringResult.Score = scoreInput.Score;
                scoringResult.Comment = scoreInput.Comment;
                scoringResult.ScoredAt = DateTime.UtcNow;
                scoringResult.UpdatedAt = DateTime.UtcNow;
                scoringResult.CurrentLevel = application.CurrentLevel;
            }

            // Lưu thay đổi vào database
            await _context.SaveChangesAsync();

            // Kiểm tra xem tất cả cán bộ đã chấm xong chưa
            var requiredScores = application.CurrentLevel == ApplicationLevel.Faculty ? 15 : 10;
            var scoredCount = await _context.ScoringResults
                .CountAsync(s => s.ApplicationId == applicationId &&
                                s.Score > 0 &&
                                s.CurrentLevel == application.CurrentLevel);

            if (scoredCount >= requiredScores)
            {
                application.Status = ApplicationStatus.Pending;
                var approver = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Level == officer.Level &&
                                            o.Role == OfficerRole.Approver &&
                                            o.IsActive);
                if (approver != null)
                {
                    _context.ApprovalDecisions.Add(new ApprovalDecision
                    {
                        ApplicationId = application.ApplicationId,
                        OfficerId = approver.Id,
                        Status = DecisionStatus.Pending.ToString(),
                        Reason = "Chờ xét duyệt",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Gửi thông báo cho sinh viên
                    var notification = new Notification
                    {
                        StudentId = application.StudentId,
                        Title = "Hồ sơ hoàn tất chấm điểm",
                        Message = $"Hồ sơ của bạn tại cấp {application.CurrentLevel} đã được chấm điểm xong và đang chờ xét duyệt.",
                        Type = "info",
                        CreatedAt = DateTime.UtcNow,
                        IsRead = false
                    };
                    await _context.Notifications.AddAsync(notification);
                }
                else
                {
                    _logger.LogWarning("Không tìm thấy cán bộ xét duyệt cho cấp {Level}", application.CurrentLevel);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Chấm điểm thành công cho ApplicationId: {ApplicationId}", applicationId);
            return Ok(new { message = "Chấm điểm thành công." });
        }

        [HttpPost]
        public async Task<IActionResult> MakeDecision(int applicationId, string decisionStatus, string reason)
        {
            var application = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Scores)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId);

            if (application == null)
            {
                _logger.LogWarning("Không tìm thấy hồ sơ với ApplicationId: {ApplicationId}", applicationId);
                return NotFound(new { message = "Không tìm thấy hồ sơ." });
            }

            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers.FindAsync(officerId);

            if (officer == null || officer.Role != OfficerRole.Approver || MapOfficerLevelToApplicationLevel(officer.Level) != application.CurrentLevel)
            {
                _logger.LogWarning("Cán bộ không có quyền xét duyệt. OfficerId: {OfficerId}, Role: {Role}, Level: {Level}",
                    officerId, officer?.Role, officer?.Level);
                return Unauthorized(new { message = "Bạn không có quyền xét duyệt hồ sơ này." });
            }

            var requiredScores = application.CurrentLevel == ApplicationLevel.Faculty ? 15 : 10;
            var scoredCount = application.Scores.Count(s => s.Score > 0 && s.CurrentLevel == application.CurrentLevel);
            if (scoredCount < requiredScores)
            {
                _logger.LogWarning("Hồ sơ chưa được chấm điểm đầy đủ. ApplicationId: {ApplicationId}, ScoredCount: {ScoredCount}, Required: {Required}",
                    applicationId, scoredCount, requiredScores);
                TempData["ErrorMessage"] = "Hồ sơ chưa được chấm điểm đầy đủ.";
                return RedirectToAction("Applications");
            }

            if (!Enum.TryParse<DecisionStatus>(decisionStatus, out var status))
            {
                _logger.LogWarning("Trạng thái quyết định không hợp lệ: {DecisionStatus}", decisionStatus);
                TempData["ErrorMessage"] = "Trạng thái quyết định không hợp lệ.";
                return RedirectToAction("Applications");
            }

            // Tạo ApprovalDecision
            var approvalDecision = new ApprovalDecision
            {
                ApplicationId = applicationId,
                OfficerId = officerId,
                Status = decisionStatus,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };
            await _context.ApprovalDecisions.AddAsync(approvalDecision);

            string notificationTitle = "";
            string notificationType = "info";
            string notificationMessage = "";

            switch (status)
            {
                case DecisionStatus.Approved:
                    application.Status = ApplicationStatus.Approved;
                    notificationTitle = "Hồ sơ được duyệt";
                    notificationType = "success";
                    notificationMessage = $"Hồ sơ của bạn đã được duyệt tại cấp {application.CurrentLevel}. Lý do: {reason}";

                    if (application.CurrentLevel != ApplicationLevel.National)
                    {
                        // Chuyển cấp tiếp theo
                        application.CurrentLevel = (ApplicationLevel)((int)application.CurrentLevel + 1);
                        application.Status = ApplicationStatus.Pending;
                        application.LastUpdated = DateTime.UtcNow;
                        application.UpdatedAt = DateTime.UtcNow;

                        // Số cán bộ chấm điểm cần thiết
                        var scorerCount = application.CurrentLevel == ApplicationLevel.Faculty ? 3 : 2;

                        // Ánh xạ ApplicationLevel sang OfficerLevel trực tiếp trong truy vấn
                        OfficerLevel targetOfficerLevel = application.CurrentLevel switch
                        {
                            ApplicationLevel.Faculty => OfficerLevel.Faculty,
                            ApplicationLevel.University => OfficerLevel.University,
                            ApplicationLevel.Province => OfficerLevel.Province,
                            ApplicationLevel.National => OfficerLevel.National,
                            _ => throw new ArgumentException($"Cấp hồ sơ không hợp lệ: {application.CurrentLevel}")
                        };

                        // Lấy danh sách cán bộ chấm điểm cho cấp tiếp theo
                        var nextLevelScorers = await _context.Officers
                            .Where(o => o.Level == targetOfficerLevel && o.Role == OfficerRole.Scorer && o.IsActive)
                            .Take(scorerCount)
                            .ToListAsync();

                        if (nextLevelScorers.Count < scorerCount)
                        {
                            _logger.LogWarning("Không đủ cán bộ chấm điểm cho cấp {Level}. Yêu cầu: {Required}, Tìm thấy: {Found}",
                                application.CurrentLevel, scorerCount, nextLevelScorers.Count);
                            TempData["ErrorMessage"] = $"Không đủ cán bộ chấm điểm cho cấp {application.CurrentLevel}.";
                            return RedirectToAction("Applications");
                        }

                        // Tạo ScoringResult cho cấp tiếp theo
                        foreach (var scorer in nextLevelScorers)
                        {
                            foreach (var appCriterion in application.ApplicationCriteria)
                            {
                                var scoringResult = new ScoringResult
                                {
                                    ApplicationId = application.ApplicationId,
                                    CriterionId = appCriterion.CriterionId,
                                    OfficerId = scorer.Id,
                                    Score = 0,
                                    Comment = "Chưa chấm",
                                    ScoredAt = DateTime.UtcNow,
                                    CurrentLevel = application.CurrentLevel
                                };
                                await _context.ScoringResults.AddAsync(scoringResult);
                            }
                        }

                        // Gửi thông báo chuyển cấp
                        var forwardNotification = new Notification
                        {
                            StudentId = application.StudentId,
                            Title = "Hồ sơ được chuyển cấp",
                            Message = $"Hồ sơ của bạn đã được chuyển lên cấp {application.CurrentLevel} để xét duyệt.",
                            Type = "info",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        await _context.Notifications.AddAsync(forwardNotification);
                    }
                    break;

                case DecisionStatus.Rejected:
                    application.Status = ApplicationStatus.Rejected;
                    application.RejectionCount++;
                    notificationTitle = "Hồ sơ bị từ chối";
                    notificationType = "danger";
                    notificationMessage = $"Hồ sơ của bạn đã bị từ chối tại cấp {application.CurrentLevel}. Lý do: {reason}";
                    application.LastUpdated = DateTime.UtcNow;
                    application.UpdatedAt = DateTime.UtcNow;

                    if (application.RejectionCount >= 2)
                    {
                        application.Status = ApplicationStatus.Rejected;
                        var lockNotification = new Notification
                        {
                            StudentId = application.StudentId,
                            Title = "Hồ sơ bị khóa",
                            Message = "Hồ sơ của bạn đã bị từ chối lần thứ 2 và không được xét duyệt thêm.",
                            Type = "danger",
                            CreatedAt = DateTime.UtcNow,
                            IsRead = false
                        };
                        await _context.Notifications.AddAsync(lockNotification);
                    }
                    break;

                case DecisionStatus.Pending:
                    application.Status = ApplicationStatus.SupplementRequested;
                    notificationTitle = "Yêu cầu bổ sung minh chứng";
                    notificationType = "warning";
                    notificationMessage = $"Hồ sơ của bạn cần bổ sung minh chứng tại cấp {application.CurrentLevel}. Lý do: {reason}";
                    application.LastUpdated = DateTime.UtcNow;
                    application.UpdatedAt = DateTime.UtcNow;
                    break;
            }

            // Gửi thông báo xét duyệt
            var notification = new Notification
            {
                StudentId = application.StudentId,
                Title = notificationTitle,
                Message = notificationMessage,
                Type = notificationType,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };
            await _context.Notifications.AddAsync(notification);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Hồ sơ đã được xét duyệt thành công! Trạng thái: {application.Status}";
            return RedirectToAction("Applications");
        }
        public async Task<IActionResult> Reports()
        {
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers
                .FirstOrDefaultAsync(o => o.Id == officerId);

            if (officer == null)
            {
                return NotFound();
            }

            var officerApplicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var applications = await _context.Applications
                .Include(a => a.Student)
                .Where(a => a.CurrentLevel == officerApplicationLevel)
                .ToListAsync();

            ViewBag.TotalApplications = applications.Count;
            ViewBag.ApprovedApplications = applications.Count(a => a.Status == ApplicationStatus.Approved);
            ViewBag.PendingApplications = applications.Count(a => a.Status == ApplicationStatus.Pending);
            ViewBag.RejectedApplications = applications.Count(a => a.Status == ApplicationStatus.Rejected);
            ViewBag.SupplementRequested = applications.Count(a => a.Status == ApplicationStatus.SupplementRequested);

            var detailedStats = applications
                .GroupBy(a => a.Student.Faculty)
                .Select(g => new
                {
                    Faculty = g.Key ?? "Không xác định",
                    Total = g.Count(),
                    Approved = g.Count(a => a.Status == ApplicationStatus.Approved),
                    Pending = g.Count(a => a.Status == ApplicationStatus.Pending),
                    Rejected = g.Count(a => a.Status == ApplicationStatus.Rejected),
                    SupplementRequested = g.Count(a => a.Status == ApplicationStatus.SupplementRequested),
                    ApprovalRate = g.Any() ? Math.Round((double)g.Count(a => a.Status == ApplicationStatus.Approved) / g.Count() * 100, 2) : 0
                })
                .ToList();

            ViewBag.DetailedStats = detailedStats;

            ViewBag.Faculties = detailedStats.Select(s => s.Faculty).ToList();
            ViewBag.FacultyStats = detailedStats.Select(s => s.Total).ToList();

            return View(applications);
        }



        public async Task<IActionResult> Workflow()
        {
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers
                .FirstOrDefaultAsync(o => o.Id == officerId);

            if (officer == null)
            {
                return NotFound();
            }

            var officerApplicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var applications = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Scores)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .Where(a => a.CurrentLevel == officerApplicationLevel)
                .OrderByDescending(a => a.SubmissionDate)
                .ToListAsync();

            return View(applications);
        }
        [HttpPost]
        public async Task<IActionResult> ExportReport(string reportType)
        {
            _logger.LogInformation($"Yêu cầu xuất báo cáo: {reportType}");

            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers
                .FirstOrDefaultAsync(o => o.Id == officerId);

            if (officer == null)
            {
                return NotFound();
            }

            var officerApplicationLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            var applications = await _context.Applications
                .Include(a => a.Student)
                .Where(a => a.CurrentLevel == officerApplicationLevel)
                .ToListAsync();

            var detailedStats = applications
                .GroupBy(a => a.Student.Faculty)
                .Select(g => new
                {
                    Faculty = g.Key ?? "Không xác định",
                    Total = g.Count(),
                    Approved = g.Count(a => a.Status == ApplicationStatus.Approved),
                    Pending = g.Count(a => a.Status == ApplicationStatus.Pending),
                    Rejected = g.Count(a => a.Status == ApplicationStatus.Rejected),
                    SupplementRequested = g.Count(a => a.Status == ApplicationStatus.SupplementRequested),
                    ApprovalRate = g.Any() ? Math.Round((double)g.Count(a => a.Status == ApplicationStatus.Approved) / g.Count() * 100, 2) : 0
                })
                .ToList();

            // Tạo PDF
            using var document = new PdfDocument();
            var page = document.AddPage();
            var gfx = XGraphics.FromPdfPage(page);
            var fontTitle = new XFont("Arial", 16, XFontStyle.Bold);
            var fontHeader = new XFont("Arial", 12, XFontStyle.Bold);
            var fontBody = new XFont("Arial", 10, XFontStyle.Regular);

            // Tiêu đề
            gfx.DrawString($"Báo cáo Sinh viên 5 Tốt - Cấp {officerApplicationLevel}", fontTitle, XBrushes.Black, new XRect(0, 50, page.Width, 20), XStringFormats.Center);

            // Tổng quan
            gfx.DrawString("Tổng quan", fontHeader, XBrushes.Black, new XRect(50, 100, page.Width, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Tổng số hồ sơ: {applications.Count}", fontBody, XBrushes.Black, new XRect(50, 120, page.Width, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Đã duyệt: {applications.Count(a => a.Status == ApplicationStatus.Approved)}", fontBody, XBrushes.Black, new XRect(50, 140, page.Width, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Đang chờ duyệt: {applications.Count(a => a.Status == ApplicationStatus.Pending)}", fontBody, XBrushes.Black, new XRect(50, 160, page.Width, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Bị từ chối: {applications.Count(a => a.Status == ApplicationStatus.Rejected)}", fontBody, XBrushes.Black, new XRect(50, 180, page.Width, 20), XStringFormats.TopLeft);
            gfx.DrawString($"Cần bổ sung: {applications.Count(a => a.Status == ApplicationStatus.SupplementRequested)}", fontBody, XBrushes.Black, new XRect(50, 200, page.Width, 20), XStringFormats.TopLeft);

            // Thống kê chi tiết theo khoa
            gfx.DrawString("Thống kê theo khoa", fontHeader, XBrushes.Black, new XRect(50, 240, page.Width, 20), XStringFormats.TopLeft);
            int yPosition = 260;
            gfx.DrawString("Khoa | Tổng số | Đã duyệt | Đang chờ | Từ chối | Cần bổ sung | Tỷ lệ duyệt (%)", fontHeader, XBrushes.Black, new XRect(50, yPosition, page.Width, 20), XStringFormats.TopLeft);
            yPosition += 20;
            foreach (var stat in detailedStats)
            {
                gfx.DrawString($"{stat.Faculty} | {stat.Total} | {stat.Approved} | {stat.Pending} | {stat.Rejected} | {stat.SupplementRequested} | {stat.ApprovalRate}", fontBody, XBrushes.Black, new XRect(50, yPosition, page.Width, 20), XStringFormats.TopLeft);
                yPosition += 20;
            }

            // Lưu PDF vào MemoryStream
            using var stream = new MemoryStream();
            document.Save(stream, false);
            stream.Position = 0;

            return File(stream.ToArray(), "application/pdf", $"Report_{reportType}_{officerApplicationLevel}_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }
        [HttpGet]
        public async Task<IActionResult> ApplicationDetails(int id)
        {
            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            var officer = await _context.Officers.FirstOrDefaultAsync(o => o.Id == officerId);
            if (officer == null)
                return NotFound(new { message = "Không tìm thấy thông tin cán bộ." });

            var application = await _context.Applications
                .Include(a => a.Student)
                .Include(a => a.Evidences)
                .Include(a => a.Scores)
                .ThenInclude(s => s.Criterion)
                .Include(a => a.Scores)
                .ThenInclude(s => s.Officer)
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == id &&
                                          a.CurrentLevel == MapOfficerLevelToApplicationLevel(officer.Level));

            if (application == null)
                return NotFound(new { message = "Không tìm thấy hồ sơ hoặc bạn không có quyền xem." });

            return Json(new
            {
                student = new
                {
                    application.Student.StudentId,
                    application.Student.FullName,
                    application.Student.Faculty,
                    application.Student.Class
                },
                submissionDate = application.SubmissionDate,
                status = application.Status,
                scores = application.Scores.Select(s => new
                {
                    criterion = new { name = s.Criterion.Name },
                    score = s.Score,
                    comment = s.Comment,
                    officer = new { fullName = s.Officer.FullName } // ← thêm dòng này

                }),
                evidences = application.Evidences.Select(e => new
                {
                    fileName = e.Criterion.Name,
                    fileUrl = e.FilePath
                })
            });
        }
        [HttpGet("/Officer/GetCriteria/{applicationId}")]
        public async Task<IActionResult> GetCriteria(int applicationId)
        {
            _logger.LogInformation("Gọi GetCriteria cho ApplicationId: {ApplicationId}", applicationId);

            var officerId = int.Parse(User.FindFirst("OfficerId")?.Value ?? "0");
            _logger.LogInformation("OfficerId: {OfficerId}", officerId);
            var officer = await _context.Officers.FirstOrDefaultAsync(o => o.Id == officerId);
            if (officer == null)
            {
                _logger.LogWarning("Không tìm thấy cán bộ với OfficerId: {OfficerId}", officerId);
                return NotFound(new { message = "Không tìm thấy thông tin cán bộ." });
            }

            if (officer.Role != OfficerRole.Scorer)
            {
                _logger.LogWarning("Cán bộ không có quyền chấm điểm. OfficerId: {OfficerId}, Role: {Role}", officerId, officer.Role);
                return Unauthorized(new { message = "Bạn không có quyền chấm điểm hồ sơ này." });
            }

            var officerLevel = MapOfficerLevelToApplicationLevel(officer.Level);
            _logger.LogInformation("Officer Level: {Level}, Mapped ApplicationLevel: {ApplicationLevel}", officer.Level, officerLevel);

            var application = await _context.Applications
                .Include(a => a.ApplicationCriteria)
                .ThenInclude(ac => ac.Criterion)
                .FirstOrDefaultAsync(a => a.ApplicationId == applicationId &&
                                         a.CurrentLevel == officerLevel);

            if (application == null)
            {
                _logger.LogWarning("Không tìm thấy hồ sơ với ApplicationId: {ApplicationId} hoặc CurrentLevel không khớp. Expected Level: {ExpectedLevel}", applicationId, officerLevel);
                return NotFound(new { message = "Không tìm thấy hồ sơ hoặc bạn không có quyền truy cập." });
            }

            _logger.LogInformation("Tìm thấy hồ sơ với ApplicationId: {ApplicationId}, CurrentLevel: {CurrentLevel}", applicationId, application.CurrentLevel);

            // Kiểm tra xem cán bộ có được phân công chấm điểm không
            var isAssigned = await _context.ScoringResults
                .AnyAsync(s => s.Application.ApplicationId == applicationId && s.Officer.Id == officerId);
            if (!isAssigned)
            {
                _logger.LogWarning("Cán bộ không được phân công chấm điểm cho ApplicationId: {ApplicationId}, OfficerId: {OfficerId}", applicationId, officerId);
                return Unauthorized(new { message = "Bạn không được phân công chấm điểm cho hồ sơ này." });
            }

            // Lấy tất cả tiêu chí được gán cho hồ sơ
            var criteria = application.ApplicationCriteria
                .Select(ac => new
                {
                    id = ac.Criterion.Id,
                    name = ac.Criterion.Name,
                    maxScore = ac.Criterion.MaxScore
                })
                .Distinct()
                .ToList();

            if (!criteria.Any())
            {
                _logger.LogWarning("Không có tiêu chí nào được gán cho hồ sơ ApplicationId: {ApplicationId}", applicationId);
                return BadRequest(new { message = "Hồ sơ không có tiêu chí nào để chấm điểm." });
            }

            _logger.LogInformation("Tìm thấy {Count} tiêu chí cho ApplicationId: {ApplicationId}", criteria.Count, applicationId);
            return Ok(criteria);
        }

    }
    public class ScoreInput
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int CriterionId { get; set; }

        [Required]
        [Range(0, 100)]
        public double Score { get; set; }

        [MaxLength(1000)]
        public string Comment { get; set; }
    }
}