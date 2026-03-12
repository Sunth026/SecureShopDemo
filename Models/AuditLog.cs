using System.ComponentModel.DataAnnotations;

namespace SecureShopDemo.Models {
    public class AuditLog {
        public int Id { get; set; }

        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [MaxLength(200)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}