using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Controllers
{
    public class AdminController : Controller
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

        // Trang tổng quan Admin
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // Danh sách sản phẩm
        public ActionResult ProductList()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

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
                    c.CategoryName
                FROM dbo.Products p
                INNER JOIN dbo.Categories c
                    ON p.CategoryID = c.CategoryID
                ORDER BY p.ProductID DESC";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
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
                            CategoryName = reader["CategoryName"].ToString()
                        });
                    }
                }
            }

            return View(products);
        }

        // Lấy danh sách danh mục cho dropdown
        private void LoadCategories()
        {
            List<SelectListItem> categories = new List<SelectListItem>();

            string sql = @"
                SELECT CategoryID, CategoryName
                FROM dbo.Categories
                ORDER BY CategoryID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
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
        // GET: Admin/CategoryList
        // Hiển thị danh sách danh mục
        public ActionResult CategoryList()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            List<Category> categories = new List<Category>();

            string sql = @"
        SELECT CategoryID, CategoryName
        FROM dbo.Categories
        ORDER BY CategoryID ASC";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            CategoryID = Convert.ToInt32(reader["CategoryID"]),
                            CategoryName = reader["CategoryName"].ToString()
                        });
                    }
                }
            }

            return View(categories);
        }
        // GET: Admin/CreateCategory
        public ActionResult CreateCategory()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(Category category)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Vui lòng nhập tên danh mục."
                );

                return View(category);
            }

            string sql = @"
        INSERT INTO dbo.Categories (CategoryName)
        VALUES (@CategoryName)";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CategoryName", SqlDbType.NVarChar, 100)
                    .Value = category.CategoryName.Trim();

                connection.Open();
                command.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Thêm danh mục thành công!";

            return RedirectToAction("CategoryList");
        }
        // GET: Admin/EditCategory
        public ActionResult EditCategory(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            Category category = null;

            string sql = @"
        SELECT CategoryID, CategoryName
        FROM dbo.Categories
        WHERE CategoryID = @CategoryID";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = id;
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        category = new Category
                        {
                            CategoryID = Convert.ToInt32(reader["CategoryID"]),
                            CategoryName = reader["CategoryName"].ToString()
                        };
                    }
                }
            }

            if (category == null)
            {
                return HttpNotFound();
            }

            return View(category);
        }

        // POST: Admin/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(Category category)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Vui lòng nhập tên danh mục."
                );

                return View(category);
            }

            string sql = @"
        UPDATE dbo.Categories
        SET CategoryName = @CategoryName
        WHERE CategoryID = @CategoryID";

            int rowsAffected;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CategoryName", SqlDbType.NVarChar, 100)
                    .Value = category.CategoryName.Trim();

                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = category.CategoryID;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
            {
                return HttpNotFound();
            }

            TempData["SuccessMessage"] = "Cập nhật danh mục thành công!";

            return RedirectToAction("CategoryList");
        }
        // POST: Admin/DeleteCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra danh mục có sản phẩm đang sử dụng hay không
            string checkSql = @"
        SELECT COUNT(*)
        FROM dbo.Products
        WHERE CategoryID = @CategoryID";

            int productCount;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(checkSql, connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = id;

                connection.Open();
                productCount = (int)command.ExecuteScalar();
            }

            // Không cho xóa nếu danh mục vẫn còn sản phẩm
            if (productCount > 0)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa danh mục này vì vẫn còn sản phẩm đang sử dụng!";

                return RedirectToAction("CategoryList");
            }

            // Xóa danh mục nếu không có sản phẩm liên kết
            string deleteSql = @"
        DELETE FROM dbo.Categories
        WHERE CategoryID = @CategoryID";

            int rowsAffected;

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(deleteSql, connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
            {
                return HttpNotFound();
            }

            TempData["SuccessMessage"] = "Xóa danh mục thành công!";

            return RedirectToAction("CategoryList");
        }


        // GET: Admin/CreateProduct
        public ActionResult CreateProduct()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            LoadCategories();

            Product product = new Product
            {
                IsActive = true
            };

            return View(product);
        }

        // POST: Admin/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateProduct(Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra dữ liệu nhập
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Giá sản phẩm phải lớn hơn 0.");
            }

            if (product.CategoryID <= 0)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục.");
            }

            // Kiểm tra danh mục có tồn tại trong database không
            bool categoryExists = false;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Categories WHERE CategoryID = @CategoryID",
                connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = product.CategoryID;

                connection.Open();

                categoryExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
            }

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                LoadCategories();
                return View(product);
            }

            string sql = @"
                INSERT INTO dbo.Products
                (
                    ProductName,
                    Price,
                    ImageUrl,
                    CategoryID,
                    Description,
                    IsActive
                )
                VALUES
                (
                    @ProductName,
                    @Price,
                    @ImageUrl,
                    @CategoryID,
                    @Description,
                    @IsActive
                )";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProductName", SqlDbType.NVarChar, 255)
                    .Value = product.ProductName.Trim();

                SqlParameter priceParameter =
                    command.Parameters.Add("@Price", SqlDbType.Decimal);
                priceParameter.Precision = 18;
                priceParameter.Scale = 2;
                priceParameter.Value = product.Price;

                command.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, -1)
                    .Value = string.IsNullOrWhiteSpace(product.ImageUrl)
                        ? (object)DBNull.Value
                        : product.ImageUrl.Trim();

                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = product.CategoryID;

                command.Parameters.Add("@Description", SqlDbType.NVarChar, -1)
                    .Value = string.IsNullOrWhiteSpace(product.Description)
                        ? (object)DBNull.Value
                        : product.Description.Trim();

                command.Parameters.Add("@IsActive", SqlDbType.Bit)
                    .Value = product.IsActive;

                connection.Open();
                command.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";


            return RedirectToAction("ProductList");
        }

        // GET: Admin/EditProduct/5
        // Lấy thông tin sản phẩm cần sửa
        public ActionResult EditProduct(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            Product product = null;

            string sql = @"
                SELECT
                    ProductID,
                    ProductName,
                    Price,
                    ImageUrl,
                    CategoryID,
                    Description,
                    IsActive
                FROM dbo.Products
                WHERE ProductID = @ProductID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProductID", SqlDbType.Int)
                    .Value = id;

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
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };
                    }
                }
            }

            if (product == null)
            {
                return HttpNotFound();
            }

            LoadCategories();

            return View(product);
        }

        // POST: Admin/EditProduct
        // Cập nhật thông tin sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProduct(Product product)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            // Kiểm tra dữ liệu
            if (string.IsNullOrWhiteSpace(product.ProductName))
            {
                ModelState.AddModelError(
                    "ProductName",
                    "Vui lòng nhập tên sản phẩm.");
            }

            if (product.Price <= 0)
            {
                ModelState.AddModelError(
                    "Price",
                    "Giá sản phẩm phải lớn hơn 0.");
            }

            if (product.CategoryID <= 0)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Vui lòng chọn danh mục.");
            }

            // Kiểm tra danh mục tồn tại
            bool categoryExists = false;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Categories WHERE CategoryID = @CategoryID",
                connection))
            {
                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = product.CategoryID;

                connection.Open();

                categoryExists = Convert.ToInt32(command.ExecuteScalar()) > 0;
            }

            if (!categoryExists)
            {
                ModelState.AddModelError(
                    "CategoryID",
                    "Danh mục không tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                LoadCategories();
                return View(product);
            }

            string sql = @"
                UPDATE dbo.Products
                SET
                    ProductName = @ProductName,
                    Price = @Price,
                    ImageUrl = @ImageUrl,
                    CategoryID = @CategoryID,
                    Description = @Description,
                    IsActive = @IsActive
                WHERE ProductID = @ProductID";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProductID", SqlDbType.Int)
                    .Value = product.ProductID;

                command.Parameters.Add("@ProductName", SqlDbType.NVarChar, 255)
                    .Value = product.ProductName.Trim();

                SqlParameter priceParameter =
                    command.Parameters.Add("@Price", SqlDbType.Decimal);
                priceParameter.Precision = 18;
                priceParameter.Scale = 2;
                priceParameter.Value = product.Price;

                command.Parameters.Add("@ImageUrl", SqlDbType.NVarChar, -1)
                    .Value = string.IsNullOrWhiteSpace(product.ImageUrl)
                        ? (object)DBNull.Value
                        : product.ImageUrl.Trim();

                command.Parameters.Add("@CategoryID", SqlDbType.Int)
                    .Value = product.CategoryID;

                command.Parameters.Add("@Description", SqlDbType.NVarChar, -1)
                    .Value = string.IsNullOrWhiteSpace(product.Description)
                        ? (object)DBNull.Value
                        : product.Description.Trim();

                command.Parameters.Add("@IsActive", SqlDbType.Bit)
                    .Value = product.IsActive;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
            {
                return HttpNotFound();
            }

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công!";

            return RedirectToAction("ProductList");
        }


        // POST: Admin/ToggleProductStatus
        // Ẩn hoặc hiện sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleProductStatus(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

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
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProductID", SqlDbType.Int)
                    .Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
            {
                return HttpNotFound();
            }

            TempData["SuccessMessage"] =
                "Cập nhật trạng thái sản phẩm thành công!";

            return RedirectToAction("ProductList");
        }

        // GET: Admin/OrderList
        // Hiển thị danh sách tất cả đơn hàng
        public ActionResult OrderList()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            List<OrderListViewModel> orders =
                new List<OrderListViewModel>();

            string sql = @"
                SELECT
                    o.OrderID,
                    o.UserID,
                    u.FullName AS CustomerName,
                    o.ReceiverName,
                    o.ReceiverPhone,
                    o.ShippingAddress,
                    o.TotalAmount,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.OrderStatus,
                    o.CreatedAt
                FROM dbo.Orders o
                INNER JOIN dbo.Users u
                    ON o.UserID = u.UserID
                ORDER BY o.CreatedAt DESC";

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
                        orders.Add(new OrderListViewModel
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            UserID = Convert.ToInt32(reader["UserID"]),
                            CustomerName = reader["CustomerName"].ToString(),
                            ReceiverName = reader["ReceiverName"].ToString(),
                            ReceiverPhone = reader["ReceiverPhone"].ToString(),
                            ShippingAddress = reader["ShippingAddress"].ToString(),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            PaymentMethod = reader["PaymentMethod"].ToString(),
                            PaymentStatus = reader["PaymentStatus"].ToString(),
                            OrderStatus = reader["OrderStatus"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        });
                    }
                }
            }

            return View(orders);
        }
        // GET: Admin/OrderDetails/1
        public ActionResult OrderDetails(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            Order order = null;

            string sqlOrder = @"
        SELECT
            o.*,
            u.FullName AS CustomerName
        FROM dbo.Orders o
        INNER JOIN dbo.Users u ON o.UserID = u.UserID
        WHERE o.OrderID = @OrderID";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sqlOrder, connection))
            {
                command.Parameters.AddWithValue("@OrderID", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        order = new Order
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            UserID = Convert.ToInt32(reader["UserID"]),
                            ReceiverName = reader["ReceiverName"].ToString(),
                            ReceiverPhone = reader["ReceiverPhone"].ToString(),
                            ShippingAddress = reader["ShippingAddress"].ToString(),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                            PaymentMethod = reader["PaymentMethod"].ToString(),
                            PaymentStatus = reader["PaymentStatus"].ToString(),
                            OrderStatus = reader["OrderStatus"].ToString(),
                            Note = reader["Note"] == DBNull.Value
                                ? ""
                                : reader["Note"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                        };

                        ViewBag.CustomerName = reader["CustomerName"].ToString();
                    }
                }
            }

            if (order == null)
            {
                return HttpNotFound();
            }

            string sqlDetails = @"
        SELECT
            OrderDetailID,
            OrderID,
            ProductID,
            ProductName,
            Quantity,
            UnitPrice
        FROM dbo.OrderDetails
        WHERE OrderID = @OrderID";

            order.OrderDetails = new List<OrderDetail>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sqlDetails, connection))
            {
                command.Parameters.AddWithValue("@OrderID", id);
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        order.OrderDetails.Add(new OrderDetail
                        {
                            OrderDetailID = Convert.ToInt32(reader["OrderDetailID"]),
                            OrderID = Convert.ToInt32(reader["OrderID"]),
                            ProductID = Convert.ToInt32(reader["ProductID"]),
                            ProductName = reader["ProductName"].ToString(),
                            Quantity = Convert.ToInt32(reader["Quantity"]),
                            UnitPrice = Convert.ToDecimal(reader["UnitPrice"])
                        });
                    }
                }
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int orderId, string orderStatus)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Index", "Home");
            }

            // Danh sách trạng thái hợp lệ
            string[] allowedStatuses =
            {
        "Chờ xác nhận",
        "Đang xử lý",
        "Đang giao",
        "Đã giao",
        "Đã hủy"
    };

            // Kiểm tra trạng thái được gửi lên
            if (string.IsNullOrWhiteSpace(orderStatus) ||
                !allowedStatuses.Contains(orderStatus))
            {
                TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }

            string sql = @"
        UPDATE dbo.Orders
        SET OrderStatus = @OrderStatus
        WHERE OrderID = @OrderID";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            using (SqlCommand command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@OrderStatus", orderStatus);
                command.Parameters.AddWithValue("@OrderID", orderId);

                connection.Open();

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = "Cập nhật trạng thái đơn hàng thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                }
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

    }
}
