using System.ComponentModel.DataAnnotations;

namespace web_sport_gear.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(200, ErrorMessage = "Tên người nhận không được vượt quá 200 ký tự.")]
        public string ReceiverName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(40, ErrorMessage = "Số điện thoại không được vượt quá 40 ký tự.")]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(1000, ErrorMessage = "Địa chỉ không được vượt quá 1000 ký tự.")]
        public string ShippingAddress { get; set; }

        [StringLength(1000, ErrorMessage = "Ghi chú không được vượt quá 1000 ký tự.")]
        public string Note { get; set; }
    }
}