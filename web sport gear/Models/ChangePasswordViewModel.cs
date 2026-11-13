
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace web_sport_gear.Models
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [StringLength(128, ErrorMessage = "Mật khẩu hiện tại không được vượt quá 128 ký tự.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Mật khẩu mới phải có từ 8 đến 128 ký tự.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).{8,128}$",
            ErrorMessage = "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [StringLength(128,
            ErrorMessage = "Mật khẩu xác nhận không được vượt quá 128 ký tự.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword",
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }
    }
}
