using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Models;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers
{
    public class ProductController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProductController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private bool IsLoggedIn()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("Username"));
        }

        private int? GetCurrentUserId()
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return null;

            return int.Parse(userIdString);
        }

        public async Task<IActionResult> Index(string? keyword)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                query = query.Where(x => x.Name.Contains(keyword) ||
                                         (x.Description != null && x.Description.Contains(keyword)));
            }

            var products = await query.ToListAsync();
            ViewBag.Keyword = keyword;

            return View(products);
        }

        public async Task<IActionResult> Details(int id, bool unsafeMode = false)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == id);
            if (product == null)
                return NotFound();

            var comments = await _context.ProductComments
                .Where(x => x.ProductId == id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            bool hasPurchased = false;
            var currentUserId = GetCurrentUserId();

            if (currentUserId.HasValue)
            {
                hasPurchased = await _context.OrderItems
                    .Include(x => x.Order)
                    .AnyAsync(x => x.ProductId == id
                                && x.Order.UserId == currentUserId.Value
                                && x.Order.Status == "Completed");
            }

            var vm = new ProductDetailsViewModel
            {
                Product = product,
                Comments = comments,
                UnsafeMode = unsafeMode,
                HasPurchased = hasPurchased
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int productId, string content, IFormFile? attachment, bool unsafeMode = false)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var username = HttpContext.Session.GetString("Username") ?? "Guest";
            var currentUserId = GetCurrentUserId();

            if (!currentUserId.HasValue)
                return RedirectToAction("Login", "Account");

            var hasPurchased = await _context.OrderItems
                .Include(x => x.Order)
                .AnyAsync(x => x.ProductId == productId
                            && x.Order.UserId == currentUserId.Value
                            && x.Order.Status == "Completed");

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "Bạn chỉ có thể bình luận sau khi đã mua sản phẩm.";
                return RedirectToAction("Details", new { id = productId, unsafeMode });
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Nội dung bình luận không được để trống.";
                return RedirectToAction("Details", new { id = productId, unsafeMode });
            }

            string? savedPath = null;
            string? originalFileName = null;

            if (attachment != null && attachment.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".txt" };
                var ext = Path.GetExtension(attachment.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(ext))
                {
                    TempData["ErrorMessage"] = "File đính kèm không hợp lệ. Chỉ cho phép .jpg, .jpeg, .png, .pdf, .txt";
                    return RedirectToAction("Details", new { id = productId, unsafeMode });
                }

                if (attachment.Length > 2 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "File đính kèm vượt quá 2MB.";
                    return RedirectToAction("Details", new { id = productId, unsafeMode });
                }

                var folder = Path.Combine(_environment.WebRootPath, "comment-files");
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                var fileName = Guid.NewGuid().ToString() + ext;
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }

                savedPath = "/comment-files/" + fileName;
                originalFileName = attachment.FileName;
            }

            var comment = new ProductComment
            {
                ProductId = productId,
                Username = username,
                Content = content,
                AttachmentPath = savedPath,
                OriginalFileName = originalFileName,
                CreatedAt = DateTime.Now
            };

            _context.ProductComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Bình luận thành công.";
            return RedirectToAction("Details", new { id = productId, unsafeMode });
        }
    }
}