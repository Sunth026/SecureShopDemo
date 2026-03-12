using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Models;
using SecureShopDemo.Services;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers {
    public class SecurityController : Controller {
        private readonly AppDbContext _context;
        private readonly DdosProtectionService _ddosService;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(AppDbContext context, DdosProtectionService ddosService, ILogger<SecurityController> logger) {
            _context = context; _ddosService = ddosService; _logger = logger;
        }

        private bool IsAdmin() => HttpContext.Session.GetString("Role") == "Admin";

        private IActionResult? CheckAdmin() {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("Username"))) {
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để tiếp tục.";
                return RedirectToAction("Login", "Account");
            }
            if (!IsAdmin()) {
                _logger.LogWarning("[UNAUTHORIZED] User '{User}' attempted Admin access from {IP}",
                    HttpContext.Session.GetString("Username"), HttpContext.Connection.RemoteIpAddress);
                HttpContext.Session.Clear();
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập. Phiên đăng nhập đã bị huỷ.";
                return RedirectToAction("Login", "Account");
            }
            return null;
        }

        public async Task<IActionResult> Dashboard() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            ViewBag.TotalLogin = await _context.LoginAttemptLogs.CountAsync();
            ViewBag.FailLogin = await _context.LoginAttemptLogs.CountAsync(x => !x.IsSuccess);
            ViewBag.SuccessLogin = await _context.LoginAttemptLogs.CountAsync(x => x.IsSuccess);
            ViewBag.TotalAttacks = await _context.AttackLogs.CountAsync();
            ViewBag.BlockedIpCount = _ddosService.GetBlockedIps().Count;
            ViewBag.TotalUploads = await _context.UploadedFileLogs.CountAsync();
            ViewBag.UnsafeUploads = await _context.UploadedFileLogs.CountAsync(x => !x.IsSafe);
            ViewBag.SqlInjectionCount = await _context.AttackLogs.CountAsync(x => x.AttackType == "SQLInjection");
            ViewBag.DdosCount = await _context.AttackLogs.CountAsync(x => x.AttackType == "DDoS Suspected");
            ViewBag.TotalAuditLogs = await _context.AuditLogs.CountAsync(); // ✅ Thêm mới

            var recentLoginLogs = await _context.LoginAttemptLogs
                .OrderByDescending(x => x.AttemptTime).Take(10).ToListAsync();

            return View(recentLoginLogs);
        }

        public IActionResult Lab() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            return View();
        }

        public async Task<IActionResult> LoginLogs() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            var logs = await _context.LoginAttemptLogs.OrderByDescending(x => x.AttemptTime).Take(100).ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> AttackLogs() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            var logs = await _context.AttackLogs.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
            return View(logs);
        }

        public IActionResult BlockedIps() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            var blockedIps = _ddosService.GetBlockedIps()
                .Select(x => new BlockedIpViewModel { IpAddress = x.Key, BlockedUntil = x.Value.ToLocalTime() })
                .OrderByDescending(x => x.BlockedUntil).ToList();
            return View(blockedIps);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UnblockIp(string ip) {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            if (!string.IsNullOrEmpty(ip)) {
                _ddosService.UnblockIp(ip);
                _logger.LogInformation("[ADMIN] IP {IP} unblocked by {Admin}", ip, HttpContext.Session.GetString("Username"));
                TempData["SuccessMessage"] = $"Đã bỏ chặn IP: {ip}";
            }
            return RedirectToAction("BlockedIps");
        }

        [HttpGet]
        public IActionResult SqlInjectionDemo(bool unsafeMode = false) {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            ViewBag.UnsafeMode = unsafeMode;
            ViewBag.ExecutedSql = string.Empty;
            return View(new List<User>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SqlInjectionDemo(string username, bool unsafeMode = false) {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            ViewBag.UnsafeMode = unsafeMode;
            var result = new List<User>();

            if (string.IsNullOrWhiteSpace(username)) {
                ViewBag.ExecutedSql = string.Empty;
                return View(result);
            }

            if (unsafeMode) {
                string sql = $"SELECT * FROM Users WHERE Username = '{username}'";
                ViewBag.ExecutedSql = sql;
                try {
                    result = _context.Users.FromSqlRaw(sql).ToList();
                    if (IsSqlInjectionAttempt(username)) {
                        _context.AttackLogs.Add(new AttackLog {
                            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            AttackType = "SQLInjection",
                            Path = "/Security/SqlInjectionDemo",
                            Method = "POST",
                            Description = $"SQL Injection attempt (UNSAFE): '{username}'",
                            CreatedAt = DateTime.Now
                        });
                        _context.SaveChanges();
                        ViewBag.AttackDetected = true;
                        ViewBag.AttackMessage = $"⚠️ SQL Injection phát hiện! Input: '{username}'";
                    }
                } catch (Exception ex) { ViewBag.SqlError = $"Lỗi SQL: {ex.Message}"; }
            } else {
                ViewBag.ExecutedSql = $"SELECT * FROM Users WHERE Username = @p0  -- @p0 = '{username}'";
                try {
                    result = _context.Users.FromSqlInterpolated($"SELECT * FROM Users WHERE Username = {username}").ToList();
                    if (IsSqlInjectionAttempt(username)) {
                        ViewBag.AttackDetected = false;
                        ViewBag.AttackMessage = $"✅ SQL Injection bị chặn! Parameterized Query bảo vệ thành công.";
                    }
                } catch (Exception ex) { ViewBag.SqlError = $"Lỗi SQL: {ex.Message}"; }
            }

            return View(result);
        }

        // ✅ Thêm mới: Demo Clickjacking
        public IActionResult ClickjackingDemo() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            return View();
        }

        // ✅ Thêm mới: Xem Audit Logs
        public async Task<IActionResult> AuditLogs() {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;
            var logs = await _context.AuditLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();
            return View(logs);
        }

        private static bool IsSqlInjectionAttempt(string input) {
            if (string.IsNullOrEmpty(input)) return false;
            var patterns = new[] { "'", "--", ";", "OR ", "or ", "AND ", "and ",
                "DROP ", "drop ", "DELETE ", "delete ", "INSERT ", "insert ",
                "UPDATE ", "update ", "UNION ", "union ", "SELECT ", "select ",
                "EXEC ", "exec ", "xp_", "sp_" };
            return patterns.Any(p => input.Contains(p, StringComparison.OrdinalIgnoreCase));
        }
    }
}