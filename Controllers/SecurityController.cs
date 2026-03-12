using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Services;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers
{
    public class SecurityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly DdosProtectionService _ddosService;

        public SecurityController(AppDbContext context, DdosProtectionService ddosService)
        {
            _context = context;
            _ddosService = ddosService;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        private IActionResult? CheckAdmin()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập khu vực quản trị bảo mật.";
                return RedirectToAction("Login", "Account");
            }

            return null;
        }

        public async Task<IActionResult> Dashboard()
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            ViewBag.TotalLogin = await _context.LoginAttemptLogs.CountAsync();
            ViewBag.FailLogin = await _context.LoginAttemptLogs.CountAsync(x => x.IsSuccess == false);
            ViewBag.SuccessLogin = await _context.LoginAttemptLogs.CountAsync(x => x.IsSuccess == true);
            ViewBag.TotalAttacks = await _context.AttackLogs.CountAsync();
            ViewBag.BlockedIpCount = _ddosService.GetBlockedIps().Count;

            var recentLoginLogs = await _context.LoginAttemptLogs
                .OrderByDescending(x => x.AttemptTime)
                .Take(10)
                .ToListAsync();

            return View(recentLoginLogs);
        }

        public IActionResult Lab()
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            return View();
        }

        public async Task<IActionResult> LoginLogs()
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            var logs = await _context.LoginAttemptLogs
                .OrderByDescending(x => x.AttemptTime)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }

        public async Task<IActionResult> AttackLogs()
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            var logs = await _context.AttackLogs
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }

        public IActionResult BlockedIps()
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            var blockedIps = _ddosService.GetBlockedIps()
                .Select(x => new BlockedIpViewModel
                {
                    IpAddress = x.Key,
                    BlockedUntil = x.Value.ToLocalTime()
                })
                .OrderByDescending(x => x.BlockedUntil)
                .ToList();

            return View(blockedIps);
        }

        [HttpGet]
        public IActionResult SqlInjectionDemo(bool unsafeMode = false)
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            ViewBag.UnsafeMode = unsafeMode;
            return View(new List<Models.User>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SqlInjectionDemo(string username, bool unsafeMode = false)
        {
            var redirect = CheckAdmin();
            if (redirect != null) return redirect;

            ViewBag.UnsafeMode = unsafeMode;

            List<Models.User> result = new();

            if (string.IsNullOrWhiteSpace(username))
                return View(result);

            if (unsafeMode)
            {
                string sql = $"SELECT * FROM Users WHERE Username = '{username}'";

                result = _context.Users
                    .FromSqlRaw(sql)
                    .ToList();
            }
            else
            {
                result = _context.Users
                    .FromSqlInterpolated($"SELECT * FROM Users WHERE Username = {username}")
                    .ToList();
            }

            return View(result);
        }
    }
}