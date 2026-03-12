using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services
{
    public class OtpService
    {
        private readonly AppDbContext _context;

        public OtpService(AppDbContext context)
        {
            _context = context;
        }

        public string GenerateOtp()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        public async Task<string> CreateOtpAsync(string username)
        {
            var otp = GenerateOtp();

            var oldOtps = await _context.OtpCodes
                .Where(x => x.Username == username && !x.IsUsed)
                .ToListAsync();

            foreach (var item in oldOtps)
            {
                item.IsUsed = true;
            }

            var newOtp = new OtpEntry
            {
                Username = username,
                OtpCode = otp,
                ExpiredAt = DateTime.Now.AddMinutes(2),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.OtpCodes.Add(newOtp);
            await _context.SaveChangesAsync();

            return otp;
        }

        public async Task<bool> VerifyOtpAsync(string username, string otpCode)
        {
            var otp = await _context.OtpCodes
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x =>
                    x.Username == username &&
                    x.OtpCode == otpCode &&
                    !x.IsUsed);

            if (otp == null)
                return false;

            if (otp.ExpiredAt < DateTime.Now)
                return false;

            otp.IsUsed = true;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}