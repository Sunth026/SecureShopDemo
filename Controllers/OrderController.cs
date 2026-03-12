using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SecureShopDemo.Data;
using SecureShopDemo.Models;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers
{
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartKey = "Cart";

        public OrderController(AppDbContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            return HttpContext.Session.GetString("Username") != null;
        }

        private List<CartItemViewModel> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartKey);
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItemViewModel>();

            return JsonConvert.DeserializeObject<List<CartItemViewModel>>(cartJson) ?? new List<CartItemViewModel>();
        }

        [HttpGet]
        public IActionResult CsrfDemo()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            return View();
        }

        [HttpPost]
        public IActionResult UnsafeCheckout()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            TempData["Message"] = "Đặt hàng thành công ở chế độ UNSAFE (không có Anti-Forgery Token).";
            return RedirectToAction("CsrfDemo");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SafeCheckout()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            TempData["Message"] = "Đặt hàng thành công ở chế độ SAFE (có Anti-Forgery Token).";
            return RedirectToAction("CsrfDemo");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var userIdString = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdString))
                return RedirectToAction("Login", "Account");

            int userId = int.Parse(userIdString);
            var cart = GetCart();

            if (!cart.Any())
            {
                TempData["ErrorMessage"] = "Giỏ hàng đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(x => x.Total),
                Status = "Completed",
                ShippingAddress = "Demo Address"
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                });
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.Remove(CartKey);

            TempData["SuccessMessage"] = "Đặt hàng thành công.";
            return RedirectToAction("Success", new { orderId = order.Id });
        }

        public IActionResult Success(int orderId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            ViewBag.OrderId = orderId;
            return View();
        }
    }
}