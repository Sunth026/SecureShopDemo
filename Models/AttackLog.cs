using System.ComponentModel.DataAnnotations;

namespace SecureShopDemo.Models
{
    public class AttackLog
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string IpAddress { get; set; }

        public string AttackType { get; set; }

        public string Path { get; set; }

        public string Method { get; set; }

        public string Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}