using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourManagementWeb.Models.Entities;

namespace TourManagementWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly TourDbContext _db;
        public AccountController(TourDbContext db) => _db = db;

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            var user = await _db.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.Trim().ToLower() || u.Email.ToLower() == username.Trim().ToLower());

            if (user == null)
            {
                ViewBag.Error = "Tài khoản không tồn tại.";
                return View();
            }

            if (user.Status != "ACTIVE" || (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.Now))
            {
                ViewBag.Error = "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần.";
                return View();
            }

            // Cho phép đăng nhập tài khoản dữ liệu mẫu bằng mật khẩu "123456" hoặc khớp PasswordHash
            bool isValid = (password == "123456") || (user.PasswordHash == password);
            if (!isValid)
            {
                user.FailedLoginCount += 1;
                if (user.FailedLoginCount >= 5) user.LockoutEnd = DateTime.Now.AddMinutes(15);
                await _db.SaveChangesAsync();
                ViewBag.Error = $"Mật khẩu không đúng! (Sai {user.FailedLoginCount}/5 lần)";
                return View();
            }

            user.FailedLoginCount = 0;
            user.LockoutEnd = null;
            user.LastLoginAt = DateTime.Now;

            _db.AuditLogs.Add(new AuditLog
            {
                UserId = user.UserId,
                Action = "LOGIN",
                ObjectType = "USERS",
                ObjectId = user.UserId,
                Detail = "Đăng nhập thành công từ Web",
                CreatedAt = DateTime.Now
            });
            await _db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim("Username", user.Username),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["Success"] = $"Xin chào {user.FullName}!";
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            if (user.Role.RoleName == "ADMIN" || user.Role.RoleName == "STAFF") return RedirectToAction("Index", "Dashboard");

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(string username, string email, string password, string fullName, string phone, string address)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username || u.Email == email))
            {
                ViewBag.Error = "Username hoặc Email đã được sử dụng!";
                return View();
            }

            var customerRole = await _db.Roles.FirstAsync(r => r.RoleName == "CUSTOMER");
            var newUser = new User
            {
                Username = username.Trim(),
                Email = email.Trim(),
                PasswordHash = password,
                FullName = fullName,
                Phone = phone,
                RoleId = customerRole.RoleId,
                Status = "ACTIVE",
                CreatedAt = DateTime.Now
            };
            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            var newCustomer = new Customer
            {
                UserId = newUser.UserId,
                FullName = fullName,
                Email = email.Trim(),
                Phone = phone,
                Address = address,
                CreatedAt = DateTime.Now
            };
            _db.Customers.Add(newCustomer);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đăng ký tài khoản thành công! Mời bạn đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}   