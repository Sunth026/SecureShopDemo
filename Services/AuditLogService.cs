using SecureShopDemo.Data;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services {
    public class AuditLogService {
        private readonly AppDbContext _context;

        public AuditLogService(AppDbContext context) {
            _context = context;
        }

        public async Task LogAsync(string username, string action, string entityType,
            string? description = null, string? ip = null) {
            var log = new AuditLog {
                Username = username,
                Action = action,
                EntityType = entityType,
                Description = description,
                IpAddress = ip,
                CreatedAt = DateTime.Now
            };
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}