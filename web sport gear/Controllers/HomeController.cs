
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace web_sport_gear.Controllers
{
    // Thông tin sản phẩm dùng cho chức năng tìm kiếm
    public class ProductSearchItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
    }

    public class HomeController : Controller
    {
        // Danh sách sản phẩm hiện có trong Details.cshtml
        private List<ProductSearchItem> GetProducts()
        {
            return new List<ProductSearchItem>
            {
                new ProductSearchItem {
                    Id = 1,
                    Name = "Giày Tennis/Pickleball Nike Zoom Vapor 12",
                    Brand = "NIKE",
                    Category = "Giày Thể Thao",
                    Price = 1500000,
                    Image = "https://images.unsplash.com/photo-1542291026-7eec264c27ff?w=500"
                },
                new ProductSearchItem {
                    Id = 18,
                    Name = "Giày Chạy Bộ Nam Adidas Supernova",
                    Brand = "ADIDAS",
                    Category = "Giày Chạy Bộ",
                    Price = 1350000,
                    Image = "https://images.unsplash.com/photo-1562183241-b937e95585b6?w=500"
                },
                new ProductSearchItem {
                    Id = 17,
                    Name = "Giày Đá Banh Sân Cỏ Nhân Tạo Pro",
                    Brand = "PUMA",
                    Category = "Giày Đá Bóng",
                    Price = 890000,
                    Image = "https://images.unsplash.com/photo-1511886929837-354d827aae26?q=80&w=764"
                },
                new ProductSearchItem {
                    Id = 15,
                    Name = "Giày Thể Thao Nam Asics Court FF 3",
                    Brand = "ASICS",
                    Category = "Giày Thể Thao",
                    Price = 1650000,
                    Image = "https://images.unsplash.com/photo-1579338559194-a162d19bf842?w=500"
                },
                new ProductSearchItem {
                    Id = 2,
                    Name = "Áo Thun Thể Thao Nam Dry-Fit",
                    Brand = "GEAR SPORT",
                    Category = "Áo Thể Thao",
                    Price = 350000,
                    Image = "https://images.unsplash.com/photo-1581655353564-df123a1eb820?w=500"
                },
                new ProductSearchItem {
                    Id = 12,
                    Name = "Áo Khoác Gió Thể Thao Nam",
                    Brand = "ADIDAS",
                    Category = "Áo Khoác Nam",
                    Price = 550000,
                    Image = "https://images.unsplash.com/photo-1611308725032-74f0a551d018?w=500"
                },
                new ProductSearchItem {
                    Id = 4,
                    Name = "Bình Nước Thể Thao Cao Cấp 1L",
                    Brand = "GEAR SPORT",
                    Category = "Phụ Kiện",
                    Price = 180000,
                    Image = "https://images.unsplash.com/photo-1602143407151-7111542de6e8?w=500"
                },
                new ProductSearchItem {
                    Id = 5,
                    Name = "Bóng Đá Tiêu Chuẩn FIFA Pro",
                    Brand = "MOLTEN",
                    Category = "Phụ Kiện Bóng Đá",
                    Price = 650000,
                    Image = "https://images.unsplash.com/photo-1518091043644-c1d4457512c6?w=500"
                },
                new ProductSearchItem {
                    Id = 6,
                    Name = "Găng Tay Thủ Môn Dính Xương",
                    Brand = "PRO GOAL",
                    Category = "Phụ Kiện Thủ Môn",
                    Price = 450000,
                    Image = "https://plus.unsplash.com/premium_photo-1781205755096-6caab3faf2a1?w=500"
                },
                new ProductSearchItem {
                    Id = 13,
                    Name = "Áo Polo Thể Thao Nam Lịch Lãm",
                    Brand = "NIKE",
                    Category = "Áo Polo",
                    Price = 290000,
                    Image = "https://plus.unsplash.com/premium_photo-1747861981617-1512bc2c06e8?w=500"
                }
            };
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";
            return View();
        }

        public ActionResult Products()
        {
            return View();
        }

        public ActionResult Cart()
        {
            return View();
        }

        public ActionResult Checkout()
        {
            return View();
        }

        public ActionResult AoTheThao()
        {
            return View();
        }

        public ActionResult GiayDaBong()
        {
            return View();
        }

        public ActionResult PhuKien()
        {
            return View();
        }

        public ActionResult Details(int? id)
        {
            ViewBag.ProductId = id ?? 1;
            return View();
        }

        // Tìm kiếm sản phẩm theo tên, thương hiệu hoặc danh mục
        [HttpGet]
        public ActionResult Search(string q)
        {
            q = (q ?? "").Trim();

            var products = GetProducts();

            var results = string.IsNullOrWhiteSpace(q)
                ? new List<ProductSearchItem>()
                : products.Where(p =>
                    p.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Brand.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0
                  ).ToList();

            ViewBag.Keyword = q;
            return View(results);
        }
    }
}
