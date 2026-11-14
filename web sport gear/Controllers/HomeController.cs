
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["GearSportDB"].ConnectionString;

        // Trang chủ
        public ActionResult Index()
        {
            try
            {
                ViewBag.ProductStocks = GetProductStocks();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Lỗi lấy tồn kho: " + ex.Message
                );

                // Không cho phép thêm hàng nếu không lấy được tồn kho
                ViewBag.ProductStocks = new Dictionary<int, int>();
            }

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";
            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your application description page.";
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

        // Chuyển đến trang áo thể thao
        public ActionResult AoTheThao()
        {
            return RedirectToAction("AOTHETHAO", "Product");
        }

        // Chuyển đến trang giày thể thao
        public ActionResult GiayDaBong()
        {
            return RedirectToAction("GiayDaBong", "Product");
        }

        // Chuyển đến trang phụ kiện
        public ActionResult PhuKien()
        {
            return RedirectToAction("PHUKIEN", "Product");
        }

        // Xem chi tiết sản phẩm theo ProductID
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return HttpNotFound();
            }

            web_sport_gear.Models.Product product = null;

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT
                        p.ProductID,
                        p.ProductName,
                        p.Price,
                        p.ImageUrl,
                        p.CategoryID,
                        p.Description,
                        p.IsActive,
                        p.StockQuantity,
                        c.CategoryName
                    FROM dbo.Products p
                    LEFT JOIN dbo.Categories c
                        ON p.CategoryID = c.CategoryID
                    WHERE p.ProductID = @ProductID";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@ProductID", SqlDbType.Int
                    ).Value = id.Value;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product = new web_sport_gear.Models.Product
                            {
                                ProductID = Convert.ToInt32(
                                    reader["ProductID"]
                                ),

                                ProductName =
                                    reader["ProductName"].ToString(),

                                Price = Convert.ToDecimal(
                                    reader["Price"]
                                ),

                                ImageUrl = reader["ImageUrl"] == DBNull.Value
                                    ? ""
                                    : reader["ImageUrl"].ToString(),

                                CategoryID = Convert.ToInt32(
                                    reader["CategoryID"]
                                ),

                                Description = reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),

                                IsActive = Convert.ToBoolean(
                                    reader["IsActive"]
                                ),

                                StockQuantity = Convert.ToInt32(
                                    reader["StockQuantity"]
                                ),

                                CategoryName = reader["CategoryName"] == DBNull.Value
                                    ? "Sản phẩm"
                                    : reader["CategoryName"].ToString()
                            };
                        }
                    }
                }
            }

            if (product == null)
            {
                return HttpNotFound();
            }

            return View(product);
        }

        // Tìm kiếm sản phẩm trực tiếp từ SQL Server
        [HttpGet]
        public ActionResult Search(string q)
        {
            q = (q ?? "").Trim();

            List<ProductSearchItem> results =
                new List<ProductSearchItem>();

            if (!string.IsNullOrWhiteSpace(q))
            {
                string sql = @"
                    SELECT
                        p.ProductID,
                        p.ProductName,
                        p.Price,
                        p.ImageUrl,
                        p.SubCategory,
                        c.CategoryName
                    FROM dbo.Products p
                    LEFT JOIN dbo.Categories c
                        ON p.CategoryID = c.CategoryID
                    WHERE p.IsActive = 1
                        AND (
                            p.ProductName LIKE @Keyword
                            OR ISNULL(p.SubCategory, '') LIKE @Keyword
                            OR ISNULL(c.CategoryName, '') LIKE @Keyword
                        )
                    ORDER BY p.ProductID DESC";

                using (SqlConnection connection =
                    new SqlConnection(connectionString))
                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@Keyword", SqlDbType.NVarChar, 200
                    ).Value = "%" + q + "%";

                    connection.Open();

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            results.Add(new ProductSearchItem
                            {
                                Id = Convert.ToInt32(
                                    reader["ProductID"]
                                ),

                                Name = reader["ProductName"].ToString(),

                                Brand = "",

                                Category = reader["SubCategory"] == DBNull.Value
                                    ? (reader["CategoryName"] == DBNull.Value
                                        ? "Sản phẩm"
                                        : reader["CategoryName"].ToString())
                                    : reader["SubCategory"].ToString(),

                                Price = Convert.ToDecimal(
                                    reader["Price"]
                                ),

                                Image = reader["ImageUrl"] == DBNull.Value
                                    ? ""
                                    : reader["ImageUrl"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.Keyword = q;

            return View(results);
        }

        // Lấy số lượng tồn kho của các sản phẩm hiển thị trên trang chủ
        private Dictionary<int, int> GetProductStocks()
        {
            var stocks = new Dictionary<int, int>();

            int[] productIds = {
                1, 18, 17, 15, 2, 9, 4, 5, 6, 16
            };

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT ProductID, StockQuantity
                    FROM dbo.Products
                    WHERE ProductID IN (1, 18, 17, 15, 2, 9, 4, 5, 6, 16)
                        AND IsActive = 1";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int id = Convert.ToInt32(
                            reader["ProductID"]
                        );

                        int stock = Convert.ToInt32(
                            reader["StockQuantity"]
                        );

                        stocks[id] = stock;
                    }
                }
            }

            return stocks;
        }
    }
}
