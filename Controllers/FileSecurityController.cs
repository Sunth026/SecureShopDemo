using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Models;
using SecureShopDemo.Services;

namespace SecureShopDemo.Controllers
{
    public class FileSecurityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileScanService _fileScanService;
        private readonly IWebHostEnvironment _environment;

        public FileSecurityController(
            AppDbContext context,
            FileScanService fileScanService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _fileScanService = fileScanService;
            _environment = environment;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("Username") != null;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("Role") == "Admin";
        }

        [HttpGet]
        public IActionResult UploadDemo()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDemo(IFormFile uploadFile)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            if (uploadFile == null)
            {
                ViewBag.Message = "Vui lòng chọn file.";
                ViewBag.IsSafe = false;
                return View();
            }

            var result = _fileScanService.Validate(uploadFile);
            var username = HttpContext.Session.GetString("Username") ?? "Guest";

            var log = new UploadedFileLog
            {
                FileName = Guid.NewGuid().ToString(),
                OriginalFileName = uploadFile.FileName,
                Extension = result.Extension,
                Size = uploadFile.Length,
                IsSafe = result.IsSafe,
                ScanMessage = result.Message,
                UploadedBy = username,
                UploadedAt = DateTime.Now
            };

            if (result.IsSafe)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var savedFileName = Guid.NewGuid().ToString() + result.Extension;
                var filePath = Path.Combine(uploadsFolder, savedFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadFile.CopyToAsync(stream);
                }

                log.FileName = savedFileName;

                ViewBag.Message = $"Upload thành công: {uploadFile.FileName}";
                ViewBag.IsSafe = true;
                ViewBag.UploadedPath = "/uploads/" + savedFileName;
            }
            else
            {
                ViewBag.Message = result.Message;
                ViewBag.IsSafe = false;
            }

            _context.UploadedFileLogs.Add(log);
            await _context.SaveChangesAsync();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> UploadLogs()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem log upload.";
                return RedirectToAction("Login", "Account");
            }

            var logs = await _context.UploadedFileLogs
                .OrderByDescending(x => x.UploadedAt)
                .Take(100)
                .ToListAsync();

            return View(logs);
        }
    }
}