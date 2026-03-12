using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SecureShopDemo.Data;
using SecureShopDemo.ViewModels;

namespace SecureShopDemo.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartKey = "Cart";

        public CartController(AppDbContext context)
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

        private void SaveCart(List<CartItemViewModel> cart)
        {
            HttpContext.Session.SetString(CartKey, JsonConvert.SerializeObject(cart));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(int productId, int quantity)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            if (quantity < 1)
                quantity = 1;

            var product = _context.Products.FirstOrDefault(x => x.Id == productId);
            if (product == null)
                return NotFound();

            var cart = GetCart();
            var existing = cart.FirstOrDefault(x => x.ProductId == productId);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            TempData["SuccessMessage"] = "Đã thêm vào giỏ hàng.";
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = GetCart();
            return View(cart);
        }

        public IActionResult Remove(int productId)
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductId == productId);

            if (item != null)
                cart.Remove(item);

            SaveCart(cart);
            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Login", "Account");

            HttpContext.Session.Remove(CartKey);
            return RedirectToAction("Index");
        }
    }
}