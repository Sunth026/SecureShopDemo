namespace SecureShopDemo.ViewModels
{
    public class BlockedIpViewModel
    {
        public string IpAddress { get; set; } = string.Empty;
        public DateTime BlockedUntil { get; set; }
    }
}