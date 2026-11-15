
using System.ComponentModel.DataAnnotations;

namespace web_sport_gear.Models
{
    // VIEWMODEL DUNG CHO CHUC NANG THEM TAI KHOAN
    public class AdminUserCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Họ và tên phải có từ 2 đến 100 ký tự.")]
        [ValidPersonName(
            ErrorMessage = "Họ và tên chỉ được chứa chữ cái, dấu tiếng Việt, dấu cách, dấu nháy hoặc dấu gạch nối.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [StringLength(255,
            ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(
            @"^0[0-9]{9}$",
            ErrorMessage = "Số điện thoại phải có đúng 10 chữ số và bắt đầu bằng 0.")]
        public string Phone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(128, MinimumLength = 8,
            ErrorMessage = "Mật khẩu phải có từ 8 đến 128 ký tự.")]
        [RegularExpression(
            @"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).{8,128}$",
            ErrorMessage = "Mật khẩu phải có chữ hoa, chữ số và ký tự đặc biệt.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu.")]
        [Compare("Password",
            ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [RegularExpression(
            @"^(Admin|Customer)$",
            ErrorMessage = "Vai trò không hợp lệ.")]
        public string Role { get; set; }
    }

    // VIEWMODEL DUNG CHO CHUC NANG SUA THONG TIN
    public class AdminUserEditViewModel
    {
        public int UserID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ và tên.")]
        [StringLength(100, MinimumLength = 2,
            ErrorMessage = "Họ và tên phải có từ 2 đến 100 ký tự.")]
        [ValidPersonName(
            ErrorMessage = "Họ và tên chỉ được chứa chữ cái, dấu tiếng Việt, dấu cách, dấu nháy hoặc dấu gạch nối.")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email.")]
        [StringLength(255,
            ErrorMessage = "Email không được vượt quá 255 ký tự.")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(
            @"^0[0-9]{9}$",
            ErrorMessage = "Số điện thoại phải có đúng 10 chữ số và bắt đầu bằng 0.")]
        public string Phone { get; set; }
    }
}
