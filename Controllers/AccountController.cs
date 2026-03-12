using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Services;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers {
    public class AccountController : Controller {
        private readonly AppDbContext _context;
        private readonly LoginLogService _logService;
        private readonly OtpService _otpService;
        private readonly EmailService _emailService;

        public AccountController(
            AppDbContext context,
            LoginLogService logService,
            OtpService otpService,
            EmailService emailService) {
            _context = context;
            _logService = logService;
            _otpService = otpService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model) {
            if (!ModelState.IsValid)
                return View(model);

            var existedUser = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == model.Username);

            if (existedUser != null) {
                ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                return View(model);
            }

            var user = new Models.User {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                Role = "User",
                IsLocked = false,
                LockedUntil = null,
                CreatedAt = DateTime.Now
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model) {
            if (!ModelState.IsValid)
                return View(model);

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == model.Username);

            if (user == null) {
                await _logService.LogAsync(model.Username, ip, false, "User không tồn tại");
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");
                return View(model);
            }

            if (user.IsLocked && user.LockedUntil.HasValue && user.LockedUntil > DateTime.Now) {
                await _logService.LogAsync(user.Username, ip, false, "Tài khoản đang bị khóa");
                ModelState.AddModelError("", $"Tài khoản đang bị khóa đến {user.LockedUntil:dd/MM/yyyy HH:mm:ss}");
                return View(model);
            }

            bool isPasswordValid;
            try {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            } catch {
                await _logService.LogAsync(user.Username, ip, false, "PasswordHash không hợp lệ trong DB");
                ModelState.AddModelError("", "Dữ liệu mật khẩu không hợp lệ. Hãy kiểm tra lại dữ liệu hệ thống.");
                return View(model);
            }

            if (!isPasswordValid) {
                await _logService.LogAsync(user.Username, ip, false, "Sai mật khẩu");

                var failCount = await _context.LoginAttemptLogs
                    .CountAsync(x => x.Username == user.Username
                                  && x.IsSuccess == false
                                  && x.AttemptTime > DateTime.Now.AddMinutes(-5));

                if (failCount >= 5) {
                    user.IsLocked = true;
                    user.LockedUntil = DateTime.Now.AddMinutes(5);
                    await _context.SaveChangesAsync();

                    ModelState.AddModelError("", "Tài khoản bị khóa 5 phút do đăng nhập sai quá nhiều lần");
                    return View(model);
                }

                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu");
                return View(model);
            }

            user.IsLocked = false;
            user.LockedUntil = null;
            await _context.SaveChangesAsync();

            await _logService.LogAsync(user.Username, ip, true, "Đúng mật khẩu - chờ xác thực OTP");

            if (string.IsNullOrWhiteSpace(user.Email)) {
                ModelState.AddModelError("", "Tài khoản chưa có email đ�� nhận OTP.");
                return View(model);
            }

            var otp = await _otpService.CreateOtpAsync(user.Username);

            try {
                await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otp);
            } catch (Exception ex) {
                await _logService.LogAsync(user.Username, ip, false, "Không gửi được email OTP");
                ModelState.AddModelError("", $"Không gửi được email OTP: {ex.Message}");
                return View(model);
            }

            HttpContext.Session.Remove("UserId");
            HttpContext.Session.Remove("Username");
            HttpContext.Session.Remove("FullName");
            HttpContext.Session.Remove("Role");

            HttpContext.Session.SetString("PendingUsername", user.Username);

            TempData["SuccessMessage"] = $"Đã gửi mã OTP đến email: {user.Email}";
            return RedirectToAction("VerifyOtp");
        }

        [HttpGet]
        public IActionResult VerifyOtp() {
            var pendingUsername = HttpContext.Session.GetString("PendingUsername");

            if (string.IsNullOrEmpty(pendingUsername))
                return RedirectToAction("Login");

            var model = new VerifyOtpViewModel {
                Username = pendingUsername
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model) {
            var pendingUsername = HttpContext.Session.GetString("PendingUsername");
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

            if (string.IsNullOrEmpty(pendingUsername))
                return RedirectToAction("Login");

            model.Username = pendingUsername;

            if (!ModelState.IsValid)
                return View(model);

            // ✅ Xử lý kết quả từ OtpService với brute force protection
            var result = await _otpService.VerifyOtpAsync(pendingUsername, model.OtpCode);

            if (!result.IsValid) {
                await _logService.LogAsync(pendingUsername, ip, false, result.Message);

                // Nếu OTP bị vô hiệu hóa → bắt đăng nhập lại
                if (result.Message.Contains("vô hiệu hóa")) {
                    HttpContext.Session.Remove("PendingUsername");
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", result.Message);
                return View(model);
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Username == pendingUsername);

            if (user == null) {
                ModelState.AddModelError("", "Không tìm thấy người dùng");
                return View(model);
            }

            await _logService.LogAsync(user.Username, ip, true, "Đăng nhập thành công bằng OTP");

            HttpContext.Session.Remove("PendingUsername");

            HttpContext.Session.SetString("UserId", user.Id.ToString());
            HttpContext.Session.SetString("Username", user.Username);
            HttpContext.Session.SetString("FullName", user.FullName);
            HttpContext.Session.SetString("Role", user.Role);

            TempData["SuccessMessage"] = "Đăng nhập thành công";

            if (user.Role == "Admin")
                return RedirectToAction("Dashboard", "Security");

            return RedirectToAction("Index", "Product");
        }

        public IActionResult Logout() {
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất";
            return RedirectToAction("Login");
        }
    }
}