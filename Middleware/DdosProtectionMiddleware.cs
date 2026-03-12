using SecureShopDemo.Services;

namespace SecureShopDemo.Middleware {
    public class DdosProtectionMiddleware {
        private readonly RequestDelegate _next;

        public DdosProtectionMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, DdosProtectionService ddosService) {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var path = context.Request.Path.ToString();
            var method = context.Request.Method;

            // ✅ Bỏ qua static files để không đếm vào rate limit
            if (path.StartsWith("/lib") || path.StartsWith("/css") ||
                path.StartsWith("/js") || path.StartsWith("/images") || path.EndsWith(".ico")) {
                await _next(context); return;
            }

            // Bỏ qua khu vực admin/lab để dễ demo
            if (path.StartsWith("/Security") || path.StartsWith("/Order") ||
                path.StartsWith("/FileSecurity")) {
                await _next(context); return;
            }

            if (ddosService.IsBlocked(ip)) {
                context.Response.StatusCode = 429;
                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.WriteAsync(
                    "<h2 style='color:red;font-family:sans-serif;text-align:center;margin-top:100px'>⛔ Truy cập bị từ chối</h2>" +
                    $"<p style='text-align:center;font-family:sans-serif'>IP <strong>{ip}</strong> đã bị chặn tạm thời.<br>Vui lòng thử lại sau 1 phút.</p>");
                return;
            }

            ddosService.RecordRequest(ip, path, method);
            await _next(context);
        }
    }
}