
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Controllers
{
    public class ProductController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["GearSportDB"].ConnectionString;

        // Lấy tất cả sản phẩm đang hoạt động theo danh mục
        private List<Product> GetProductsByCategory(int categoryId)
        {
            List<Product> products = new List<Product>();

            string query = @"
                SELECT
                    p.ProductID,
                    p.ProductName,
                    p.Price,
                    p.ImageUrl,
                    p.CategoryID,
                    p.Description,
                    p.IsActive,
                    c.CategoryName
                FROM Products p
                INNER JOIN Categories c
                    ON p.CategoryID = c.CategoryID
                WHERE p.CategoryID = @CategoryID
                    AND p.IsActive = 1
                ORDER BY p.ProductID";

            using (SqlConnection connection = new SqlConnection(connectionString))
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CategoryID", categoryId);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Product product = new Product
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["ProductName"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            ImageUrl = reader["ImageUrl"].ToString(),
                            CategoryID = Convert.ToInt32(reader["CategoryID"]),
                            Description = reader["Description"] == DBNull.Value
                                ? null
                                : reader["Description"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            CategoryName = reader["CategoryName"].ToString()
                        };

                        products.Add(product);
                    }
                }
            }

            return products;
        }

        // Trang áo thể thao
        public ActionResult AOTHETHAO()
        {
            var products = GetProductsByCategory(1);
            return View("~/Views/Home/AOTHETHAO.cshtml", products);
        }

        // Trang phụ kiện
        public ActionResult PHUKIEN()
        {
            var products = GetProductsByCategory(2);
            return View("~/Views/Home/PHUKIEN.cshtml", products);
        }

        // Trang giày thể thao
        public ActionResult GiayDaBong()
        {
            var products = GetProductsByCategory(3);
            return View("~/Views/Home/GiayDaBong.cshtml", products);
        }
    }
}
