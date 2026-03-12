using System.ComponentModel.DataAnnotations;

namespace SecureShopDemo.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Mã OTP")]
        public string OtpCode { get; set; } = string.Empty;
    }
}