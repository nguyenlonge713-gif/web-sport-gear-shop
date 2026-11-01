
using System.ComponentModel.DataAnnotations;

namespace web_sport_gear.Models
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận.")]
        [StringLength(100)]
        public string ReceiverName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [StringLength(20)]
        public string ReceiverPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(500)]
        public string ShippingAddress { get; set; }

        [StringLength(500)]
        public string Note { get; set; }
    }
}
