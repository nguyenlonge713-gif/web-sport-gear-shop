
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

        private const int PageSize = 8;

        // Lấy sản phẩm theo danh mục, danh mục con và phân trang
        private List<Product> GetProductsByCategory(
            int categoryId,
            string subCategory,
            int page,
            out int totalProducts)
        {
            List<Product> products = new List<Product>();
            totalProducts = 0;

            string countQuery = @"
                SELECT COUNT(*)
                FROM Products p
                WHERE p.CategoryID = @CategoryID
                    AND p.IsActive = 1";

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
                countQuery += " AND p.SubCategory = @SubCategory";
                query += " AND p.SubCategory = @SubCategory";
            }

            query += @"
                ORDER BY p.ProductID DESC
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            using (SqlConnection connection =
                new SqlConnection(connectionString))
            {
                connection.Open();

                // Đếm tổng sản phẩm phù hợp
                using (SqlCommand countCommand =
                    new SqlCommand(countQuery, connection))
                {
                    countCommand.Parameters.AddWithValue(
                        "@CategoryID", categoryId);

                    if (!string.IsNullOrWhiteSpace(subCategory))
                    {
                        countCommand.Parameters.AddWithValue(
                            "@SubCategory", subCategory);
                    }

                    totalProducts = Convert.ToInt32(
                        countCommand.ExecuteScalar());
                }

                // Tính vị trí bắt đầu
                int offset = (page - 1) * PageSize;

                // Lấy sản phẩm của trang hiện tại
                using (SqlCommand command =
                    new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue(
                        "@CategoryID", categoryId);

                    if (!string.IsNullOrWhiteSpace(subCategory))
                    {
                        command.Parameters.AddWithValue(
                            "@SubCategory", subCategory);
                    }

                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", PageSize);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Product product = new Product
                            {
                                ProductID = Convert.ToInt32(
                                    reader["ProductID"]),

                                ProductName = reader["ProductName"].ToString(),

                                Price = Convert.ToDecimal(reader["Price"]),

                                ImageUrl = reader["ImageUrl"].ToString(),

                                CategoryID = Convert.ToInt32(
                                    reader["CategoryID"]),

                                Description = reader["Description"] == DBNull.Value
                                    ? null
                                    : reader["Description"].ToString(),

                                IsActive = Convert.ToBoolean(
                                    reader["IsActive"]),

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
            }

            return products;
        }

        // Thiết lập thông tin phân trang
        private void SetPagination(int page, int totalProducts)
        {
            int totalPages = (int)Math.Ceiling(
                (double)totalProducts / PageSize);

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalPages = totalPages;
        }

        // Trang áo thể thao
        public ActionResult AOTHETHAO(string subCategory, int page = 1)
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

            int totalProducts;

            var products = GetProductsByCategory(
                1, subCategory, page, out totalProducts);

            SetPagination(page, totalProducts);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/AOTHETHAO.cshtml", products);
        }

        // Trang phụ kiện
        public ActionResult PHUKIEN(string subCategory, int page = 1)
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

            int totalProducts;

            var products = GetProductsByCategory(
                2, subCategory, page, out totalProducts);

            SetPagination(page, totalProducts);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/PHUKIEN.cshtml", products);
        }

        // Trang giày thể thao
        public ActionResult GiayDaBong(string subCategory, int page = 1)
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

            int totalProducts;

            var products = GetProductsByCategory(
                3, subCategory, page, out totalProducts);

            SetPagination(page, totalProducts);

            ViewBag.SelectedSubCategory = subCategory;

            return View("~/Views/Home/GiayDaBong.cshtml", products);
        }
    }
}
