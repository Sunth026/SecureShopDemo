using System;

namespace SecureShopDemo.Models {
    public class OtpEntry {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public bool IsUsed { get; set; }
        public int FailAttempts { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
    }
}