namespace SecureShopDemo.Services {
    public class FileScanService {
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        private readonly string[] _blockedExtensions = { ".exe", ".bat", ".cmd", ".js", ".php", ".dll", ".msi", ".sh" };
        private readonly long _maxFileSize = 2 * 1024 * 1024;

        private static readonly Dictionary<string, byte[]> _magicBytes = new()
        {
            { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
            { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
            { ".gif",  new byte[] { 0x47, 0x49, 0x46, 0x38 } },
        };

        private static readonly Dictionary<string, byte[]> _dangerousMagicBytes = new()
        {
            { "EXE/DLL", new byte[] { 0x4D, 0x5A } },
            { "ZIP/JAR", new byte[] { 0x50, 0x4B, 0x03, 0x04 } },
            { "PDF",     new byte[] { 0x25, 0x50, 0x44, 0x46 } },
            { "ELF",     new byte[] { 0x7F, 0x45, 0x4C, 0x46 } },
        };

        // ✅ MIME type whitelist tương ứng với từng extension
        private static readonly Dictionary<string, string[]> _allowedMimeTypes = new()
        {
            { ".jpg",  new[] { "image/jpeg" } },
            { ".jpeg", new[] { "image/jpeg" } },
            { ".png",  new[] { "image/png" } },
            { ".gif",  new[] { "image/gif" } },
        };

        public (bool IsSafe, string Message, string Extension) Validate(IFormFile file) {
            if (file == null || file.Length == 0)
                return (false, "File rỗng hoặc không hợp lệ.", "");

            // Bước 1: Kiểm tra phần mở rộng
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (_blockedExtensions.Contains(extension))
                return (false, $"❌ File có phần mở rộng nguy hiểm: {extension}", extension);

            if (!_allowedExtensions.Contains(extension))
                return (false, "❌ Chỉ cho phép upload ảnh: .jpg, .jpeg, .png, .gif", extension);

            // Bước 2: Kiểm tra kích thước
            if (file.Length > _maxFileSize)
                return (false, "❌ File vượt quá 2MB.", extension);

            // Bước 3: Kiểm tra Magic Bytes
            using var stream = file.OpenReadStream();
            var headerBytes = new byte[8];
            var bytesRead = stream.Read(headerBytes, 0, headerBytes.Length);
            stream.Position = 0;

            if (bytesRead < 2)
                return (false, "❌ File quá nhỏ, không thể xác định định dạng.", extension);

            foreach (var dangerous in _dangerousMagicBytes) {
                var sig = dangerous.Value;
                if (bytesRead >= sig.Length && headerBytes.Take(sig.Length).SequenceEqual(sig))
                    return (false,
                        $"⚠️ Phát hiện file nguy hiểm ({dangerous.Key}) giả mạo thành {extension}! Magic bytes: {BitConverter.ToString(sig)}",
                        extension);
            }

            // Kiểm tra magic bytes hợp lệ theo extension
            if (_magicBytes.TryGetValue(extension, out var expectedMagic)) {
                if (bytesRead < expectedMagic.Length || !headerBytes.Take(expectedMagic.Length).SequenceEqual(expectedMagic))
                    return (false,
                        $"❌ Nội dung file không khớp với định dạng {extension} (magic bytes không hợp lệ).",
                        extension);
            }

            // Bước 4: ✅ Kiểm tra MIME type từ Content-Type header
            if (_allowedMimeTypes.TryGetValue(extension, out var allowedMimes)) {
                var contentType = file.ContentType?.ToLowerInvariant() ?? "";
                if (!allowedMimes.Contains(contentType))
                    return (false,
                        $"❌ MIME type '{file.ContentType}' không hợp lệ cho file {extension}. Cho phép: {string.Join(", ", allowedMimes)}",
                        extension);
            }

            return (true, $"✅ File hợp lệ: {file.FileName} ({extension}, {file.Length / 1024.0:F1} KB)", extension);
        }
    }
}