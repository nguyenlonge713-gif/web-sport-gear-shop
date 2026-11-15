
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;
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
                // Lấy đầy đủ thông tin sản phẩm từ SQL Server
                // Bao gồm tên, giá, hình ảnh và số lượng tồn kho
                ViewBag.HomeProducts = GetHomeProducts();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Lỗi lấy dữ liệu trang chủ: " + ex.Message
                );

                // Tránh lỗi khi không lấy được dữ liệu sản phẩm
                ViewBag.HomeProducts = new List<Product>();
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

        // Lấy danh sách danh mục từ SQL Server để hiển thị trên menu
        [ChildActionOnly]
        public ActionResult MenuCategories()
        {
            List<web_sport_gear.Models.Category> categories =
                new List<web_sport_gear.Models.Category>();

            try
            {
                string query = @"
            SELECT CategoryID, CategoryName
            FROM dbo.Categories
            ORDER BY CategoryID";

                using (SqlConnection connection =
                    new SqlConnection(connectionString))
                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            categories.Add(
                                new web_sport_gear.Models.Category
                                {
                                    CategoryID = Convert.ToInt32(
                                        reader["CategoryID"]
                                    ),

                                    CategoryName = reader["CategoryName"].ToString()
                                }
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Lỗi lấy danh mục menu: " + ex.Message
                );
            }

            return PartialView(
                "~/Views/Shared/_MenuCategories.cshtml",
                categories
            );
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


        // Xem chi tiết sản phẩm và lấy danh sách sản phẩm liên quan
        public ActionResult Details(int? id)
        {
            if (id == null || id <= 0)
            {
                return HttpNotFound();
            }

            Product product = null;
            List<Product> relatedProducts = new List<Product>();

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            {
                connection.Open();

                // 1. Lấy thông tin sản phẩm đang xem
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
                p.SubCategory,
                c.CategoryName
            FROM dbo.Products p
            LEFT JOIN dbo.Categories c
                ON p.CategoryID = c.CategoryID
            WHERE p.ProductID = @ProductID";

                using (SqlCommand command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@ProductID", SqlDbType.Int
                    ).Value = id.Value;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            product = new Product
                            {
                                ProductID = Convert.ToInt32(reader["ProductID"]),

                                ProductName = reader["ProductName"].ToString(),

                                Price = Convert.ToDecimal(reader["Price"]),

                                ImageUrl = reader["ImageUrl"] == DBNull.Value
                                    ? ""
                                    : reader["ImageUrl"].ToString(),

                                CategoryID = Convert.ToInt32(reader["CategoryID"]),

                                Description = reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),

                                IsActive = Convert.ToBoolean(reader["IsActive"]),

                                StockQuantity = Convert.ToInt32(
                                    reader["StockQuantity"]
                                ),

                                SubCategory = reader["SubCategory"] == DBNull.Value
                                    ? ""
                                    : reader["SubCategory"].ToString(),

                                CategoryName = reader["CategoryName"] == DBNull.Value
                                    ? "Sản phẩm"
                                    : reader["CategoryName"].ToString()
                            };
                        }
                    }
                }

                if (product == null)
                {
                    return HttpNotFound();
                }

                // 2. Lấy tối đa 4 sản phẩm liên quan
                // Ưu tiên cùng danh mục con, sau đó đến cùng danh mục chính.
                string relatedSql = @"
            SELECT TOP 4
                p.ProductID,
                p.ProductName,
                p.Price,
                p.ImageUrl,
                p.CategoryID,
                p.Description,
                p.IsActive,
                p.StockQuantity,
                p.SubCategory,
                c.CategoryName
            FROM dbo.Products p
            LEFT JOIN dbo.Categories c
                ON p.CategoryID = c.CategoryID
            WHERE p.CategoryID = @CategoryID
                AND p.ProductID <> @ProductID
                AND p.IsActive = 1
                AND p.StockQuantity > 0
            ORDER BY
                CASE
                    WHEN @SubCategory <> ''
                        AND p.SubCategory = @SubCategory
                    THEN 0
                    ELSE 1
                END,
                p.ProductID DESC";

                using (SqlCommand command =
                    new SqlCommand(relatedSql, connection))
                {
                    command.Parameters.Add(
                        "@CategoryID", SqlDbType.Int
                    ).Value = product.CategoryID;

                    command.Parameters.Add(
                        "@ProductID", SqlDbType.Int
                    ).Value = product.ProductID;

                    command.Parameters.Add(
                        "@SubCategory", SqlDbType.NVarChar, 255
                    ).Value = string.IsNullOrWhiteSpace(product.SubCategory)
                        ? ""
                        : product.SubCategory;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            relatedProducts.Add(new Product
                            {
                                ProductID = Convert.ToInt32(reader["ProductID"]),

                                ProductName = reader["ProductName"].ToString(),

                                Price = Convert.ToDecimal(reader["Price"]),

                                ImageUrl = reader["ImageUrl"] == DBNull.Value
                                    ? ""
                                    : reader["ImageUrl"].ToString(),

                                CategoryID = Convert.ToInt32(reader["CategoryID"]),

                                Description = reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),

                                IsActive = Convert.ToBoolean(reader["IsActive"]),

                                StockQuantity = Convert.ToInt32(
                                    reader["StockQuantity"]
                                ),

                                SubCategory = reader["SubCategory"] == DBNull.Value
                                    ? ""
                                    : reader["SubCategory"].ToString(),

                                CategoryName = reader["CategoryName"] == DBNull.Value
                                    ? "Sản phẩm"
                                    : reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }

            ViewBag.RelatedProducts = relatedProducts;

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

        // Lấy đầy đủ thông tin sản phẩm hiển thị trên trang chủ
        private List<Product> GetHomeProducts()
        {
            List<Product> products = new List<Product>();

            string sql = @"
        SELECT
            ProductID,
            ProductName,
            Price,
            ImageUrl,
            CategoryID,
            Description,
            IsActive,
            StockQuantity,
            SubCategory
        FROM dbo.Products
        WHERE ProductID IN (1, 18, 17, 15, 2, 9, 4, 5, 6, 16)
            AND IsActive = 1
        ORDER BY ProductID";

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product product = new Product
                        {
                            ProductID = Convert.ToInt32(
                                reader["ProductID"]
                            ),

                            ProductName = reader["ProductName"].ToString(),

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

                            SubCategory = reader["SubCategory"] == DBNull.Value
                                ? ""
                                : reader["SubCategory"].ToString()
                        };

                        products.Add(product);
                    }
                }
            }

            return products;
        }

      
    }
}
