using SecureShopDemo.Data;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services
{
    public class LoginLogService
    {
        private readonly AppDbContext _context;

        public LoginLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string username, string ip, bool success, string note)
        {
            var log = new LoginAttemptLog
            {
                Username = username,
                IpAddress = ip,
                IsSuccess = success,
                AttemptTime = DateTime.Now,
                Note = note
            };

            _context.LoginAttemptLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}