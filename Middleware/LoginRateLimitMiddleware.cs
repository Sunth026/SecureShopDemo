using System.Collections.Concurrent;

namespace SecureShopDemo.Middleware {
    /// <summary>
    /// Giới hạn tốc độ đăng nhập: tối đa 10 lần POST /Account/Login trong 60 giây mỗi IP
    /// Tách biệt khỏi DDoS middleware để có thể cấu hình riêng cho login endpoint
    /// </summary>
    public class LoginRateLimitMiddleware {
        private readonly RequestDelegate _next;
        private static readonly ConcurrentDictionary<string, List<DateTime>> _loginAttempts = new();
        private const int MaxAttempts = 10;
        private const int WindowSeconds = 60;

        public LoginRateLimitMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context) {
            // Chỉ áp dụng cho POST /Account/Login
            if (context.Request.Method == "POST" &&
                context.Request.Path.StartsWithSegments("/Account/Login", StringComparison.OrdinalIgnoreCase)) {
                var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
                var now = DateTime.UtcNow;

                var attempts = _loginAttempts.GetOrAdd(ip, _ => new List<DateTime>());

                lock (attempts) {
                    // Xóa các attempt cũ ngoài cửa sổ thời gian
                    attempts.RemoveAll(x => x < now.AddSeconds(-WindowSeconds));
                    attempts.Add(now);

                    if (attempts.Count > MaxAttempts) {
                        context.Response.StatusCode = 429; // Too Many Requests
                        context.Response.ContentType = "text/html; charset=utf-8";
                        context.Response.WriteAsync(
                            "<!DOCTYPE html><html><body style='font-family:sans-serif;text-align:center;margin-top:100px'>" +
                            "<h2 style='color:red'>⛔ Quá nhiều lần thử đăng nhập</h2>" +
                            "<p>Bạn đã thử đăng nhập quá nhiều lần. Vui lòng thử lại sau 1 phút.</p>" +
                            "<a href='/Account/Login'>← Quay lại</a></body></html>"
                        ).GetAwaiter().GetResult();
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}