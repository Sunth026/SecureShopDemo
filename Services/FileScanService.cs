namespace SecureShopDemo.Services
{
    public class FileScanService
    {
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly string[] _blockedExtensions = { ".exe", ".bat", ".cmd", ".js", ".php", ".dll", ".msi", ".sh" };
        private readonly long _maxFileSize = 2 * 1024 * 1024;

        public (bool IsSafe, string Message, string Extension) Validate(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return (false, "File rỗng hoặc không hợp lệ.", "");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (_blockedExtensions.Contains(extension))
                return (false, $"File có phần mở rộng nguy hiểm: {extension}", extension);

            if (!_allowedExtensions.Contains(extension))
                return (false, "Chỉ cho phép upload ảnh: .jpg, .jpeg, .png, .gif", extension);

            if (file.Length > _maxFileSize)
                return (false, "File vượt quá 2MB.", extension);

            return (true, "File an toàn.", extension);
        }
    }
}