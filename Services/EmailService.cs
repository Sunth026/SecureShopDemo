using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SecureShopDemo.Models;

namespace SecureShopDemo.Services
{
    public class EmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings)
        {
            _emailSettings = emailSettings.Value;
        }

        public async Task SendOtpEmailAsync(string toEmail, string fullName, string otpCode)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "Mã OTP đăng nhập SecureShopDemo";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <h2>Xác thực đăng nhập</h2>
                    <p>Xin chào <b>{fullName}</b>,</p>
                    <p>Mã OTP của bạn là:</p>
                    <h1 style='color:blue;'>{otpCode}</h1>
                    <p>Mã có hiệu lực trong <b>2 phút</b>.</p>
                    <p>Nếu bạn không thực hiện đăng nhập, hãy bỏ qua email này.</p>"
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_emailSettings.Username, _emailSettings.Password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}