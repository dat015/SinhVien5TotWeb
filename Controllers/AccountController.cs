using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SinhVien5TotWeb.Models;
using SinhVien5TotWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.ComponentModel.DataAnnotations;

namespace SinhVien5TotWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(AppDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "")
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra đăng nhập cho Student
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.Email == model.Email && s.Password == DbInitializer.HashPassword(model.Password));

                if (student != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, student.FullName),
                        new Claim(ClaimTypes.Email, student.Email),
                        new Claim(ClaimTypes.Role, "Student"),
                        new Claim("StudentId", student.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Dashboard", "Student");
                }

                // Kiểm tra đăng nhập cho Officer
                var officer = await _context.Officers
                    .FirstOrDefaultAsync(o => o.Email == model.Email && o.Password == DbInitializer.HashPassword(model.Password));

                if (officer != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, officer.FullName),
                        new Claim(ClaimTypes.Email, officer.Email),
                        new Claim(ClaimTypes.Role, "Officer"),
                        new Claim("OfficerId", officer.Id.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Dashboard", "Officer");
                }

                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
            }

            return View(model);
        }


        [HttpGet]
        public IActionResult Register()
        {
            // Danh sách trường đại học Việt Nam
            ViewBag.Universities = new List<string>
            {
                "Đại học Quốc gia Hà Nội",
                "Đại học Bách khoa Hà Nội",
                "Đại học Sư phạm Hà Nội",
                "Đại học Kinh tế Quốc dân",
                "Đại học Y Hà Nội",
                "Đại học Quốc gia TP.HCM",
                "Đại học Bách khoa TP.HCM",
                "Đại học Sư phạm TP.HCM",
                "Đại học Kinh tế - Luật TP.HCM",
                "Đại học Y Dược TP.HCM",
                "Đại học Cần Thơ",
                "Đại học Đà Nẵng",
                "Đại học Huế",
                "Đại học Vinh",
                "Đại học Thái Nguyên",
                // Thêm các trường khác nếu cần
            };
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra email đã tồn tại chưa
                if (await _context.Students.AnyAsync(s => s.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng.");
                    ViewBag.Universities = new List<string>
                    {
                        "Đại học Quốc gia Hà Nội",
                        "Đại học Bách khoa Hà Nội",
                        "Đại học Sư phạm Hà Nội",
                        "Đại học Kinh tế Quốc dân",
                        "Đại học Y Hà Nội",
                        "Đại học Quốc gia TP.HCM",
                        "Đại học Bách khoa TP.HCM",
                        "Đại học Sư phạm TP.HCM",
                        "Đại học Kinh tế - Luật TP.HCM",
                        "Đại học Y Dược TP.HCM",
                        "Đại học Cần Thơ",
                        "Đại học Đà Nẵng",
                        "Đại học Huế",
                        "Đại học Vinh",
                        "Đại học Thái Nguyên",
                    };
                    return View(model);
                }

                // Sinh StudentId tự động (ví dụ: SV + năm hiện tại + số thứ tự)
                string studentId;
                do
                {
                    studentId = $"SV{DateTime.Now.Year}{new Random().Next(1000, 9999)}";
                } while (await _context.Students.AnyAsync(s => s.StudentId == studentId));

                // Tạo đối tượng Student mới
                var student = new Student
                {
                    StudentId = studentId,
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = DbInitializer.HashPassword(model.Password),
                    PhoneNumber = model.PhoneNumber,
                    Class = model.Class,
                    Faculty = model.Faculty,
                    University = model.University,
                    CreatedAt = DateTime.Now
                };

                // Lưu sinh viên vào cơ sở dữ liệu
                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Tự động đăng nhập
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, student.FullName),
                    new Claim(ClaimTypes.Email, student.Email),
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim("StudentId", student.Id.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = false
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Dashboard", "Student");
            }

            // Nếu dữ liệu không hợp lệ, trả lại danh sách trường đại học
            ViewBag.Universities = new List<string>
            {
                "Đại học Quốc gia Hà Nội",
                "Đại học Bách khoa Hà Nội",
                "Đại học Sư phạm Hà Nội",
                "Đại học Kinh tế Quốc dân",
                "Đại học Y Hà Nội",
                "Đại học Quốc gia TP.HCM",
                "Đại học Bách khoa TP.HCM",
                "Đại học Sư phạm TP.HCM",
                "Đại học Kinh tế - Luật TP.HCM",
                "Đại học Y Dược TP.HCM",
                "Đại học Cần Thơ",
                "Đại học Đà Nẵng",
                "Đại học Huế",
                "Đại học Vinh",
                "Đại học Thái Nguyên",
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
}
