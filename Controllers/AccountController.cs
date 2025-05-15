using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SinhVien5TotWeb.Models;
using SinhVien5TotWeb.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
    }
} 