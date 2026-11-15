
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
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

        // Kiểm tra đăng nhập và quyền Admin
        private ActionResult CheckAdmin()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login", "Account", new { area = "" });
            }

            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index", "Home", new { area = "" });
            }

            return null;
        }

        // Trang mặc định của Orders
        public ActionResult Index()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return RedirectToAction("OrderList");
        }

        // GET: Admin/Orders/OrderList
        // Hiển thị danh sách tất cả đơn hàng
        public ActionResult OrderList()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

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

            return View("OrderList", orders);
        }

        // GET: Admin/Orders/OrderDetails/1
        // Xem chi tiết đơn hàng
        public ActionResult OrderDetails(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            Order order = null;

            string sqlOrder = @"
                SELECT
                    o.*,
                    u.FullName AS CustomerName
                FROM dbo.Orders o
                INNER JOIN dbo.Users u
                    ON o.UserID = u.UserID
                WHERE o.OrderID = @OrderID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sqlOrder, connection))
            {
                command.Parameters.Add("@OrderID", SqlDbType.Int)
                    .Value = id;

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

                        ViewBag.CustomerName =
                            reader["CustomerName"].ToString();
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

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sqlDetails, connection))
            {
                command.Parameters.Add("@OrderID", SqlDbType.Int)
                    .Value = id;

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

            return View("OrderDetails", order);
        }


        // POST: Admin/Orders/UpdateOrderStatus
        // Cập nhật trạng thái và tự động xử lý tồn kho
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(int orderId, string orderStatus)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            string[] allowedStatuses =
            {
        "Chờ xác nhận",
        "Đang xử lý",
        "Đang giao",
        "Đã giao",
        "Đã hủy"
    };

            if (string.IsNullOrWhiteSpace(orderStatus) ||
                !allowedStatuses.Contains(orderStatus))
            {
                TempData["ErrorMessage"] =
                    "Trạng thái đơn hàng không hợp lệ.";

                return RedirectToAction("OrderDetails", new { id = orderId });
            }

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // 1. Lấy trạng thái và tình trạng trừ kho hiện tại
                        // UPDLOCK, HOLDLOCK giúp tránh xử lý đồng thời cùng một đơn.
                        string checkSql = @"
                    SELECT OrderStatus, StockDeducted
                    FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                    WHERE OrderID = @OrderID";

                        string currentStatus = null;
                        bool stockDeducted = false;

                        using (SqlCommand command =
                            new SqlCommand(checkSql, connection, transaction))
                        {
                            command.Parameters.Add("@OrderID", SqlDbType.Int)
                                .Value = orderId;

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    currentStatus = reader["OrderStatus"].ToString();

                                    stockDeducted =
                                        Convert.ToBoolean(reader["StockDeducted"]);
                                }
                            }
                        }

                        if (currentStatus == null)
                        {
                            throw new Exception("Không tìm thấy đơn hàng.");
                        }

                        // 2. Không cho phép thay đổi đơn đã hủy
                        if (currentStatus == "Đã hủy" &&
                            orderStatus != "Đã hủy")
                        {
                            throw new Exception(
                                "Đơn hàng đã hủy, không thể thay đổi trạng thái.");
                        }

                        // 2.1. Không cho phép thay đổi đơn đã giao
                        if (currentStatus == "Đã giao")
                        {
                            throw new Exception(
                                "Đơn hàng đã giao, không thể thay đổi trạng thái.");
                        }

                        // 3. Khi chuyển sang ĐÃ GIAO: kiểm tra và trừ kho
                        if (orderStatus == "Đã giao" && !stockDeducted)
                        {
                            // Kiểm tra đơn hàng có sản phẩm hay không
                            string countSql = @"
                        SELECT COUNT(*)
                        FROM dbo.OrderDetails
                        WHERE OrderID = @OrderID";

                            using (SqlCommand command =
                                new SqlCommand(countSql, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int)
                                    .Value = orderId;

                                int detailCount = Convert.ToInt32(
                                    command.ExecuteScalar());

                                if (detailCount == 0)
                                {
                                    throw new Exception(
                                        "Đơn hàng không có sản phẩm để trừ kho.");
                                }
                            }

                            // Kiểm tra tồn kho của tất cả sản phẩm trong đơn
                            string stockCheckSql = @"
                        SELECT TOP 1
                            p.ProductName,
                            d.RequiredQuantity,
                            p.StockQuantity
                        FROM
                        (
                            SELECT
                                ProductID,
                                SUM(Quantity) AS RequiredQuantity
                            FROM dbo.OrderDetails
                            WHERE OrderID = @OrderID
                            GROUP BY ProductID
                        ) d
                        INNER JOIN dbo.Products p
                            WITH (UPDLOCK, HOLDLOCK)
                            ON p.ProductID = d.ProductID
                        WHERE p.StockQuantity < d.RequiredQuantity";

                            using (SqlCommand command =
                                new SqlCommand(stockCheckSql, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int)
                                    .Value = orderId;

                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        string productName =
                                            reader["ProductName"].ToString();

                                        int required = Convert.ToInt32(
                                            reader["RequiredQuantity"]);

                                        int available = Convert.ToInt32(
                                            reader["StockQuantity"]);

                                        throw new Exception(
                                            "Sản phẩm " + productName +
                                            " không đủ tồn kho. Cần: " + required +
                                            ", hiện còn: " + available + ".");
                                    }
                                }
                            }

                            // Trừ tồn kho từng sản phẩm
                            string deductSql = @"
                        UPDATE p
                        SET p.StockQuantity =
                            p.StockQuantity - d.RequiredQuantity
                        FROM dbo.Products p
                        INNER JOIN
                        (
                            SELECT
                                ProductID,
                                SUM(Quantity) AS RequiredQuantity
                            FROM dbo.OrderDetails
                            WHERE OrderID = @OrderID
                            GROUP BY ProductID
                        ) d
                            ON p.ProductID = d.ProductID
                        WHERE p.StockQuantity >= d.RequiredQuantity";

                            using (SqlCommand command =
                                new SqlCommand(deductSql, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int)
                                    .Value = orderId;

                                command.ExecuteNonQuery();
                            }

                            // Đánh dấu đơn hàng đã trừ kho
                            stockDeducted = true;
                        }

                        // 4. Khi hủy đơn: chỉ hoàn kho nếu trước đó đã trừ kho
                        if (currentStatus != "Đã hủy" &&
                            orderStatus == "Đã hủy" &&
                            stockDeducted)
                        {
                            string restoreSql = @"
                        UPDATE p
                        SET p.StockQuantity =
                            p.StockQuantity + d.RequiredQuantity
                        FROM dbo.Products p
                        INNER JOIN
                        (
                            SELECT
                                ProductID,
                                SUM(Quantity) AS RequiredQuantity
                            FROM dbo.OrderDetails
                            WHERE OrderID = @OrderID
                            GROUP BY ProductID
                        ) d
                            ON p.ProductID = d.ProductID";

                            using (SqlCommand command =
                                new SqlCommand(restoreSql, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int)
                                    .Value = orderId;

                                command.ExecuteNonQuery();
                            }

                            stockDeducted = false;
                        }

                        // 5. Cập nhật trạng thái và cờ StockDeducted
                        string updateSql = @"
                    UPDATE dbo.Orders
                    SET
                        OrderStatus = @OrderStatus,
                        StockDeducted = @StockDeducted
                    WHERE OrderID = @OrderID";

                        using (SqlCommand command =
                            new SqlCommand(updateSql, connection, transaction))
                        {
                            command.Parameters.Add(
                                "@OrderStatus", SqlDbType.NVarChar, 50)
                                .Value = orderStatus;

                            command.Parameters.Add(
                                "@StockDeducted", SqlDbType.Bit)
                                .Value = stockDeducted;

                            command.Parameters.Add(
                                "@OrderID", SqlDbType.Int)
                                .Value = orderId;

                            command.ExecuteNonQuery();
                        }

                        // 6. Hoàn tất toàn bộ giao dịch
                        transaction.Commit();

                        if (orderStatus == "Đã giao" && stockDeducted)
                        {
                            TempData["SuccessMessage"] =
                                "Cập nhật đơn hàng thành Đã giao và trừ tồn kho thành công!";
                        }
                        else if (orderStatus == "Đã hủy")
                        {
                            TempData["SuccessMessage"] =
                                "Hủy đơn hàng và xử lý hoàn kho thành công!";
                        }
                        else
                        {
                            TempData["SuccessMessage"] =
                                "Cập nhật trạng thái đơn hàng thành công!";
                        }
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        TempData["ErrorMessage"] = ex.Message;
                    }
                }
            }

            return RedirectToAction("OrderDetails", new { id = orderId });
        }

    }
}
