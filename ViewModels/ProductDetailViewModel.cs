using SecureShopDemo.Models;

namespace SecureShopDemo.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product? Product { get; set; }
        public List<ProductComment> Comments { get; set; } = new();
        public bool UnsafeMode { get; set; }
        public bool HasPurchased { get; set; }
    }
}