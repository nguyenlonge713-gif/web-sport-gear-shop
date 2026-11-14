
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

        // Lấy sản phẩm theo danh mục và danh mục con
        private List<Product> GetProductsByCategory(
            int categoryId, string subCategory = null)
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
                    p.StockQuantity,
                    p.SubCategory,
                    c.CategoryName
                FROM Products p
                INNER JOIN Categories c
                    ON p.CategoryID = c.CategoryID
                WHERE p.CategoryID = @CategoryID
                    AND p.IsActive = 1";

            if (!string.IsNullOrWhiteSpace(subCategory))
            {
                query += " AND p.SubCategory = @SubCategory";
            }

            query += " ORDER BY p.ProductID";

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            using (SqlCommand command =
                new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CategoryID", categoryId);

                if (!string.IsNullOrWhiteSpace(subCategory))
                {
                    command.Parameters.AddWithValue(
                        "@SubCategory", subCategory);
                }

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

                            StockQuantity = Convert.ToInt32(
                                reader["StockQuantity"]),

                            CategoryName = reader["CategoryName"].ToString(),

                            SubCategory = reader["SubCategory"] == DBNull.Value
                                ? null
                                : reader["SubCategory"].ToString()
                        };

                        products.Add(product);
                    }
                }
            }

            return products;
        }

        // Trang áo thể thao và lọc danh mục con
        public ActionResult AOTHETHAO(string subCategory)
        {
            string[] validCategories =
            {
                "Áo Thun Thể Thao",
                "Áo Khoác Gió",
                "Áo Polo Thể Thao"
            };

            if (!string.IsNullOrWhiteSpace(subCategory) &&
                Array.IndexOf(validCategories, subCategory) == -1)
            {
                subCategory = null;
            }

            var products = GetProductsByCategory(1, subCategory);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/AOTHETHAO.cshtml", products);
        }

        // Trang phụ kiện và lọc danh mục con
        public ActionResult PHUKIEN(string subCategory)
        {
            string[] validCategories =
            {
                  "Vợt Cầu Lông",
                  "Bóng Thể Thao",
                  "Găng Tay"
            };

            if (!string.IsNullOrWhiteSpace(subCategory) &&
                Array.IndexOf(validCategories, subCategory) == -1)
            {
                subCategory = null;
            }

            var products = GetProductsByCategory(2, subCategory);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/PHUKIEN.cshtml", products);
        }

        // Trang giày thể thao và lọc danh mục con
        public ActionResult GiayDaBong(string subCategory)
        {
            string[] validCategories =
            {
                "Giày Đá Banh",
                "Giày Chạy Bộ",
                "Giày Bóng Rổ"
            };

            if (!string.IsNullOrWhiteSpace(subCategory) &&
                Array.IndexOf(validCategories, subCategory) == -1)
            {
                subCategory = null;
            }

            var products = GetProductsByCategory(3, subCategory);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/GiayDaBong.cshtml", products);
        }
    }
}
