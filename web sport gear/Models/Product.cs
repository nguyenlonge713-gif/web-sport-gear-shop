
using System.ComponentModel.DataAnnotations;

namespace web_sport_gear.Models
{
    public class Product
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm.")]
        [StringLength(255,
            ErrorMessage = "Tên sản phẩm không được vượt quá 255 ký tự.")]
        public string ProductName { get; set; }

        [Range(typeof(decimal), "0.01", "9999999999999999.99",
            ErrorMessage = "Giá sản phẩm phải lớn hơn 0.")]
        public decimal Price { get; set; }

        [StringLength(2048,
            ErrorMessage = "Đường dẫn hình ảnh không được vượt quá 2048 ký tự.")]
        public string ImageUrl { get; set; }

        [Range(1, int.MaxValue,
            ErrorMessage = "Vui lòng chọn danh mục sản phẩm.")]
        public int CategoryID { get; set; }

        [StringLength(1000,
            ErrorMessage = "Mô tả không được vượt quá 1000 ký tự.")]
        public string Description { get; set; }

        public bool IsActive { get; set; }

        [Range(0, int.MaxValue,
            ErrorMessage = "Số lượng tồn kho không được âm.")]
        public int StockQuantity { get; set; }

        public string CategoryName { get; set; }

        public string SubCategory { get; set; }
    }
}
