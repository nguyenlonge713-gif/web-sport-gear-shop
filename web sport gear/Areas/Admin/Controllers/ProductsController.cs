using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class ProductsController : AdminBaseController
    {
        // Chuỗi kết nối database
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"]
                    .ConnectionString;
            }
        }

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return Session["UserID"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        // Kiểm tra đăng nhập và quyền truy cập
        private ActionResult CheckAdmin()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "" }
                );
            }

            return null;
        }

        // Trang mặc định của Products
        public ActionResult Index()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return RedirectToAction("ProductList");
        }

        // Danh sách sản phẩm
        public ActionResult ProductList()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            List<Product> products = new List<Product>();

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
                INNER JOIN dbo.Categories c
                    ON p.CategoryID = c.CategoryID
                ORDER BY p.ProductID DESC";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Product
                        {
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["ProductName"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),

                            ImageUrl = reader["ImageUrl"] == DBNull.Value
                                ? null
                                : reader["ImageUrl"].ToString(),

                            CategoryID = Convert.ToInt32(reader["CategoryID"]),

                            Description = reader["Description"] == DBNull.Value
                                ? null
                                : reader["Description"].ToString(),

                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            StockQuantity = Convert.ToInt32(reader["StockQuantity"]),
                            CategoryName = reader["CategoryName"].ToString()
                        });
                    }
                }
            }

            return View("~/Areas/Admin/Views/Products/ProductList.cshtml", products);
        }

        // Lấy danh sách danh mục cho dropdown
        private void LoadCategories()
        {
            List<SelectListItem> categories =
                new List<SelectListItem>();

            string sql = @"
                SELECT CategoryID, CategoryName
                FROM dbo.Categories
                ORDER BY CategoryID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new SelectListItem
                        {
                            Value = reader["CategoryID"].ToString(),
                            Text = reader["CategoryName"].ToString()
                        });
                    }
                }
            }

            ViewBag.Categories = categories;
        }

        // GET: Admin/Products/CreateProduct
        public ActionResult CreateProduct()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            LoadCategories();

            Product product = new Product
            {
                IsActive = true
            };

            return View("CreateProduct", product);
        }

        // POST: Admin/Products/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(Product product)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            // Kiểm tra tên sản phẩm
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm."
                );
            }

            // Kiểm tra giá
            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Giá sản phẩm phải lớn hơn 0."
                );
            }

            // Kiểm tra danh mục
            if (product.CategoryID <= 0)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục."
                );
            }

            // Kiểm tra danh mục có tồn tại trong database
            bool categoryExists = false;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM dbo.Categories
                  WHERE CategoryID = @CategoryID",
                connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = product.CategoryID;

                connection.Open();

                categoryExists =
                    Convert.ToInt32(command.ExecuteScalar()) > 0;
            }

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại."
                );
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return View("CreateProduct", product);
            }

            string sql = @"
                INSERT INTO dbo.Products
                (
                    ProductName,
                    Price,
                    ImageUrl,
                    CategoryID,
                    Description,
                    IsActive,
                    StockQuantity
                )
                VALUES
                (
                    @ProductName,
                    @Price,
                    @ImageUrl,
                    @CategoryID,
                    @Description,
                    @IsActive,
                    @StockQuantity
                )";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@ProductName",
                    SqlDbType.NVarChar,
                    255
                ).Value = product.ProductName.Trim();

                SqlParameter priceParameter =
                    command.Parameters.Add(
                        "@Price",
                        SqlDbType.Decimal
                    );

                priceParameter.Precision = 18;
                priceParameter.Scale = 2;
                priceParameter.Value = product.Price;

                command.Parameters.Add(
                    "@ImageUrl",
                    SqlDbType.NVarChar,
                    -1
                ).Value = string.IsNullOrWhiteSpace(product.ImageUrl)
                    ? (object)DBNull.Value
                    : product.ImageUrl.Trim();

                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = product.CategoryID;

                command.Parameters.Add(
                    "@Description",
                    SqlDbType.NVarChar,
                    -1
                ).Value = string.IsNullOrWhiteSpace(product.Description)
                    ? (object)DBNull.Value
                    : product.Description.Trim();

                command.Parameters.Add(
                    "@IsActive",
                    SqlDbType.Bit
                ).Value = product.IsActive;

                command.Parameters.Add(
                    "@StockQuantity",
                    SqlDbType.Int
                ).Value = product.StockQuantity;

                connection.Open();
                command.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "Thêm sản phẩm thành công!";

            return RedirectToAction("ProductList");
        }

        // GET: Admin/Products/EditProduct/5
        public ActionResult EditProduct(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            Product product = null;

            string sql = @"
                SELECT
                    ProductID,
                    ProductName,
                    Price,
                    ImageUrl,
                    CategoryID,
                    Description,
                    IsActive,
                    StockQuantity
                FROM dbo.Products
                WHERE ProductID = @ProductID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@ProductID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();

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
                                ? null
                                : reader["ImageUrl"].ToString(),

                            CategoryID = Convert.ToInt32(reader["CategoryID"]),

                            Description = reader["Description"] == DBNull.Value
                                ? null
                                : reader["Description"].ToString(),

                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            StockQuantity = Convert.ToInt32(reader["StockQuantity"])
                        };
                    }
                }
            }

            if (product == null)
                return HttpNotFound();

            LoadCategories();

            return View("EditProduct", product);
        }

        // POST: Admin/Products/EditProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product product)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            // Kiểm tra tên sản phẩm
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm."
                );
            }

            // Kiểm tra giá
            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Giá sản phẩm phải lớn hơn 0."
                );
            }

            // Kiểm tra số lượng tồn kho
            if (product.StockQuantity < 0)
            {
                ModelState.AddModelError(
                    "StockQuantity",
                    "Số lượng tồn kho không được âm."
                );
            }

            // Kiểm tra danh mục
            if (product.CategoryID <= 0)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục."
                );
            }

            // Kiểm tra danh mục tồn tại
            bool categoryExists = false;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                @"SELECT COUNT(*)
                  FROM dbo.Categories
                  WHERE CategoryID = @CategoryID",
                connection))
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = product.CategoryID;

                connection.Open();

                categoryExists =
                    Convert.ToInt32(command.ExecuteScalar()) > 0;
            }

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại."
                );
            }

            // Nếu dữ liệu không hợp lệ
            if (!ModelState.IsValid)
            {
                LoadCategories();
                return View("EditProduct", product);
            }

            string sql = @"
                UPDATE dbo.Products
                SET
                    ProductName = @ProductName,
                    Price = @Price,
                    ImageUrl = @ImageUrl,
                    CategoryID = @CategoryID,
                    Description = @Description,
                    IsActive = @IsActive,
                    StockQuantity = @StockQuantity
                WHERE ProductID = @ProductID";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@ProductID",
                    SqlDbType.Int
                ).Value = product.ProductID;

                command.Parameters.Add(
                    "@ProductName",
                    SqlDbType.NVarChar,
                    255
                ).Value = product.ProductName.Trim();

                SqlParameter priceParameter =
                    command.Parameters.Add(
                        "@Price",
                        SqlDbType.Decimal
                    );

                priceParameter.Precision = 18;
                priceParameter.Scale = 2;
                priceParameter.Value = product.Price;

                command.Parameters.Add(
                    "@ImageUrl",
                    SqlDbType.NVarChar,
                    -1
                ).Value = string.IsNullOrWhiteSpace(product.ImageUrl)
                    ? (object)DBNull.Value
                    : product.ImageUrl.Trim();

                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = product.CategoryID;

                command.Parameters.Add(
                    "@Description",
                    SqlDbType.NVarChar,
                    -1
                ).Value = string.IsNullOrWhiteSpace(product.Description)
                    ? (object)DBNull.Value
                    : product.Description.Trim();

                command.Parameters.Add(
                    "@IsActive",
                    SqlDbType.Bit
                ).Value = product.IsActive;

                command.Parameters.Add(
                    "@StockQuantity",
                    SqlDbType.Int
                ).Value = product.StockQuantity;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
                return HttpNotFound();

            TempData["SuccessMessage"] =
                "Cập nhật sản phẩm thành công!";

            return RedirectToAction("ProductList");
        }

        // POST: Admin/Products/ToggleProductStatus
        // Ẩn hoặc hiện sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleProductStatus(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            string sql = @"
                UPDATE dbo.Products
                SET IsActive = CASE
                    WHEN IsActive = 1 THEN 0
                    ELSE 1
                END
                WHERE ProductID = @ProductID";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@ProductID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
                return HttpNotFound();

            TempData["SuccessMessage"] =
                "Cập nhật trạng thái sản phẩm thành công!";

            return RedirectToAction("ProductList");
        }
        // POST: Admin/Products/DeleteProduct
        // Xóa sản phẩm nếu chưa có trong lịch sử đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProduct(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            string sql = @"
        DELETE FROM dbo.Products
        WHERE ProductID = @ProductID
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.OrderDetails
              WHERE OrderDetails.ProductID = dbo.Products.ProductID
          )";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@ProductID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected > 0)
            {
                TempData["SuccessMessage"] =
                    "Xóa sản phẩm thành công!";
            }
            else
            {
                // Kiểm tra sản phẩm còn tồn tại không
                bool productExists;

                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(
                    @"SELECT COUNT(*)
              FROM dbo.Products
              WHERE ProductID = @ProductID",
                    connection))
                {
                    command.Parameters.Add(
                        "@ProductID",
                        SqlDbType.Int
                    ).Value = id;

                    connection.Open();

                    productExists =
                        Convert.ToInt32(command.ExecuteScalar()) > 0;
                }

                if (!productExists)
                {
                    return HttpNotFound();
                }

                TempData["ErrorMessage"] =
                    "Không thể xóa sản phẩm vì sản phẩm đã xuất hiện trong lịch sử đơn hàng. Bạn hãy sử dụng chức năng Ẩn.";
            }

            return RedirectToAction("ProductList");
        }
    }
}