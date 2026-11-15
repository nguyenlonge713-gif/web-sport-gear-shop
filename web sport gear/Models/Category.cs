
using System.ComponentModel.DataAnnotations;

namespace web_sport_gear.Models
{
    public class Category
    {
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục.")]
        [StringLength(100, ErrorMessage = "Tên danh mục không được vượt quá 100 ký tự.")]
        public string CategoryName { get; set; }
    }
}
