
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;
using System.Text;
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

        // Kiểm tra danh mục có tồn tại trong database
        private bool CategoryExists(int categoryID)
        {
            if (categoryID <= 0)
                return false;

            string sql = @"
                SELECT COUNT(*)
                FROM dbo.Categories
                WHERE CategoryID = @CategoryID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = categoryID;

                connection.Open();

                return Convert.ToInt32(
                    command.ExecuteScalar()
                ) > 0;
            }
        }

        // Kiểm tra sản phẩm có tồn tại trong database
        private bool ProductExists(int productID)
        {
            if (productID <= 0)
                return false;

            string sql = @"
                SELECT COUNT(*)
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
                ).Value = productID;

                connection.Open();

                return Convert.ToInt32(
                    command.ExecuteScalar()
                ) > 0;
            }
        }

        // Trang mặc định của Products
        public ActionResult Index()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return RedirectToAction("ProductList");
        }

        // Danh sách sản phẩm: tìm kiếm, lọc và phân trang
        public ActionResult ProductList(
            string search,
            int? categoryId,
            string status,
            string stockStatus,
            int page = 1,
            int pageSize = 10)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            // Chuẩn hóa dữ liệu đầu vào
            search = string.IsNullOrWhiteSpace(search)
                ? ""
                : search.Trim();

            if (categoryId.HasValue && categoryId.Value <= 0)
                categoryId = null;

            // Chỉ cho phép các giá trị hợp lệ
            if (status != "active" && status != "inactive")
                status = "";

            if (stockStatus != "in" && stockStatus != "out")
                stockStatus = "";

            // Giới hạn số sản phẩm mỗi trang
            if (pageSize != 10 && pageSize != 20 && pageSize != 50)
                pageSize = 10;

            if (page < 1)
                page = 1;

            // Xây dựng điều kiện lọc SQL
            StringBuilder where = new StringBuilder(
                " WHERE 1 = 1 "
            );

            if (!string.IsNullOrWhiteSpace(search))
            {
                where.Append(
                    " AND p.ProductName LIKE @Search "
                );
            }

            if (categoryId.HasValue)
            {
                where.Append(
                    " AND p.CategoryID = @CategoryID "
                );
            }

            if (status == "active")
            {
                where.Append(
                    " AND p.IsActive = 1 "
                );
            }
            else if (status == "inactive")
            {
                where.Append(
                    " AND p.IsActive = 0 "
                );
            }

            if (stockStatus == "in")
            {
                where.Append(
                    " AND p.StockQuantity > 0 "
                );
            }
            else if (stockStatus == "out")
            {
                where.Append(
                    " AND p.StockQuantity = 0 "
                );
            }

            List<Product> products = new List<Product>();

            int totalProducts = 0;
            int totalPages = 0;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                // 1. Đếm tổng số sản phẩm sau khi áp dụng bộ lọc
                string countSql = @"
            SELECT COUNT(*)
            FROM dbo.Products p
            INNER JOIN dbo.Categories c
                ON p.CategoryID = c.CategoryID
        " + where.ToString();

                using (SqlCommand countCommand =
                    new SqlCommand(countSql, connection))
                {
                    AddProductFilterParameters(
                        countCommand,
                        search,
                        categoryId
                    );

                    totalProducts = Convert.ToInt32(
                        countCommand.ExecuteScalar()
                    );
                }

                // 2. Tính tổng số trang
                totalPages = (int)Math.Ceiling(
                    (double)totalProducts / pageSize
                );

                // Nếu không có sản phẩm, vẫn giữ trang hiện tại là 1
                if (totalPages == 0)
                    totalPages = 1;

                // Nếu trang đang chọn vượt quá tổng số trang
                if (page > totalPages)
                    page = totalPages;

                int offset = (page - 1) * pageSize;

                // 3. Lấy sản phẩm của trang hiện tại
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
        " + where.ToString() + @"
            ORDER BY p.ProductID DESC
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    AddProductFilterParameters(
                        command,
                        search,
                        categoryId
                    );

                    command.Parameters.Add(
                        "@Offset",
                        SqlDbType.Int
                    ).Value = offset;

                    command.Parameters.Add(
                        "@PageSize",
                        SqlDbType.Int
                    ).Value = pageSize;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            products.Add(new Product
                            {
                                ProductID = Convert.ToInt32(
                                    reader["ProductID"]
                                ),

                                ProductName = reader["ProductName"].ToString(),

                                Price = Convert.ToDecimal(
                                    reader["Price"]
                                ),

                                ImageUrl = reader["ImageUrl"] == DBNull.Value
                                    ? null
                                    : reader["ImageUrl"].ToString(),

                                CategoryID = Convert.ToInt32(
                                    reader["CategoryID"]
                                ),

                                Description = reader["Description"] == DBNull.Value
                                    ? null
                                    : reader["Description"].ToString(),

                                IsActive = Convert.ToBoolean(
                                    reader["IsActive"]
                                ),

                                StockQuantity = Convert.ToInt32(
                                    reader["StockQuantity"]
                                ),

                                CategoryName = reader["CategoryName"].ToString()
                            });
                        }
                    }
                }
            }

            // 4. Nạp danh mục cho bộ lọc
            LoadCategories();

            // 5. Truyền dữ liệu phân trang và bộ lọc sang View
            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.Status = status;
            ViewBag.StockStatus = stockStatus;

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.TotalPages = totalPages;

            return View(
                "~/Areas/Admin/Views/Products/ProductList.cshtml",
                products
            );
        }

        // Thêm tham số tìm kiếm và lọc vào câu lệnh SQL
        private void AddProductFilterParameters(
            SqlCommand command,
            string search,
            int? categoryId)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                command.Parameters.Add(
                    "@Search",
                    SqlDbType.NVarChar,
                    255
                ).Value = "%" + search + "%";
            }

            if (categoryId.HasValue)
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = categoryId.Value;
            }
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

                using (SqlDataReader reader =
                    command.ExecuteReader())
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

            if (product == null)
                return new HttpStatusCodeResult(400);

            // Kiểm tra danh mục tồn tại trong database
            if (product.CategoryID > 0 &&
                !CategoryExists(product.CategoryID))
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại."
                );
            }

            // Kiểm tra dữ liệu theo DataAnnotations trong Product.cs
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
                    1000
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

                using (SqlDataReader reader =
                    command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        product = new Product
                        {
                            ProductID = Convert.ToInt32(
                                reader["ProductID"]
                            ),

                            ProductName = reader["ProductName"].ToString(),

                            Price = Convert.ToDecimal(
                                reader["Price"]
                            ),

                            ImageUrl = reader["ImageUrl"] == DBNull.Value
                                ? null
                                : reader["ImageUrl"].ToString(),

                            CategoryID = Convert.ToInt32(
                                reader["CategoryID"]
                            ),

                            Description = reader["Description"] == DBNull.Value
                                ? null
                                : reader["Description"].ToString(),

                            IsActive = Convert.ToBoolean(
                                reader["IsActive"]
                            ),

                            StockQuantity = Convert.ToInt32(
                                reader["StockQuantity"]
                            )
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
        public ActionResult EditProduct(int id, Product product)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            if (product == null)
                return new HttpStatusCodeResult(400);

            // Kiểm tra ID trên URL và ID trong form có khớp không
            if (id <= 0 || product.ProductID != id)
            {
                return new HttpStatusCodeResult(
                    400,
                    "Mã sản phẩm không hợp lệ."
                );
            }

            // Kiểm tra sản phẩm có tồn tại trong database không
            if (!ProductExists(id))
            {
                return HttpNotFound();
            }

            // Kiểm tra danh mục tồn tại trong database
            if (product.CategoryID > 0 &&
                !CategoryExists(product.CategoryID))
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại."
                );
            }

            // Kiểm tra dữ liệu theo DataAnnotations trong Product.cs
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
                ).Value = id;

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
                    1000
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
                      WHERE OrderDetails.ProductID =
                            dbo.Products.ProductID
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
                using (SqlCommand command =
                    new SqlCommand(
                        @"SELECT COUNT(*)
                          FROM dbo.Products
                          WHERE ProductID = @ProductID",
                        connection
                    ))
                {
                    command.Parameters.Add(
                        "@ProductID",
                        SqlDbType.Int
                    ).Value = id;

                    connection.Open();

                    productExists = Convert.ToInt32(
                        command.ExecuteScalar()
                    ) > 0;
                }

                if (!productExists)
                    return HttpNotFound();

                TempData["ErrorMessage"] =
                    "Không thể xóa sản phẩm vì sản phẩm đã xuất hiện trong lịch sử đơn hàng. Bạn hãy sử dụng chức năng Ẩn.";
            }

            return RedirectToAction("ProductList");
        }
    }
}
