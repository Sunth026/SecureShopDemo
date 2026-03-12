using Microsoft.EntityFrameworkCore;
using SecureShopDemo.Data;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services {
    public class OtpService {
        private readonly AppDbContext _context;

        public OtpService(AppDbContext context) {
            _context = context;
        }

        // ✅ Dùng CSPRNG thay System.Random để đảm bảo entropy mật mã học
        public string GenerateOtp() {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            var bytes = new byte[4];
            rng.GetBytes(bytes);
            int otp = Math.Abs(BitConverter.ToInt32(bytes, 0)) % 900000 + 100000;
            return otp.ToString();
        }

        public async Task<string> CreateOtpAsync(string username) {
            var otp = GenerateOtp();

            // Vô hiệu hóa tất cả OTP cũ chưa dùng
            var oldOtps = await _context.OtpCodes
                .Where(x => x.Username == username && !x.IsUsed)
                .ToListAsync();

            foreach (var item in oldOtps)
                item.IsUsed = true;

            var newOtp = new OtpEntry {
                Username = username,
                OtpCode = otp,
                ExpiredAt = DateTime.Now.AddMinutes(2),
                IsUsed = false,
                FailAttempts = 0,
                CreatedAt = DateTime.Now
            };

            _context.OtpCodes.Add(newOtp);
            await _context.SaveChangesAsync();

            return otp;
        }

        // ✅ Trả về tuple (IsValid, Message) để xử lý brute force OTP
        public async Task<(bool IsValid, string Message)> VerifyOtpAsync(string username, string otpCode) {
            var otp = await _context.OtpCodes
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(x =>
                    x.Username == username &&
                    !x.IsUsed);

            if (otp == null)
                return (false, "Mã OTP không tồn tại hoặc đã được sử dụng.");

            if (otp.ExpiredAt < DateTime.Now)
                return (false, "Mã OTP đã hết hạn. Vui lòng đăng nhập lại.");

            // ✅ Chống brute force: tối đa 5 lần nhập sai
            if (otp.FailAttempts >= 5) {
                otp.IsUsed = true;
                await _context.SaveChangesAsync();
                return (false, "OTP đã bị vô hiệu hóa do nhập sai quá 5 lần. Vui lòng đăng nhập lại.");
            }

            if (otp.OtpCode != otpCode) {
                otp.FailAttempts++;
                await _context.SaveChangesAsync();
                int remaining = 5 - otp.FailAttempts;
                return (false, $"Mã OTP không đúng. Còn {remaining} lần thử.");
            }

            otp.IsUsed = true;
            await _context.SaveChangesAsync();
            return (true, "OK");
        }
    }
}