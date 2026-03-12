using SecureShopDemo.Services;

namespace SecureShopDemo.Middleware
{
    public class DdosProtectionMiddleware
    {
        private readonly RequestDelegate _next;

        public DdosProtectionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, DdosProtectionService ddosService)
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var path = context.Request.Path.ToString();
            var method = context.Request.Method;

            // Bỏ qua kiểm tra với các route quản trị/lab để dễ demo
            if (path.StartsWith("/Security") || path.StartsWith("/Order") || path.StartsWith("/FileSecurity"))
            {
                await _next(context);
                return;
            }

            if (ddosService.IsBlocked(ip))
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Too many requests. IP temporarily blocked.");
                return;
            }

            ddosService.RecordRequest(ip, path, method);

            await _next(context);
        }
    }
}