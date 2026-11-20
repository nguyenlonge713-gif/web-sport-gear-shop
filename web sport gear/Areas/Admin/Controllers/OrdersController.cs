
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class OrdersController : AdminBaseController
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"].ConnectionString;
            }
        }

        // ================= KIỂM TRA QUYỀN ADMIN =================

        private bool IsAdmin()
        {
            return Session["UserID"] != null &&
                   Session["Role"] != null &&
                   string.Equals(
                       Session["Role"].ToString(),
                       "Admin",
                       StringComparison.OrdinalIgnoreCase);
        }

        private ActionResult CheckAdmin()
        {
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" });
            }

            return null;
        }

        // ================= TRANG MẶC ĐỊNH =================

        public ActionResult Index()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return RedirectToAction("OrderList");
        }

        // ================= DANH SÁCH ĐƠN HÀNG =================

        public ActionResult OrderList(string paymentStatus)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            List<OrderListViewModel> orders =
                new List<OrderListViewModel>();

            bool filterPaidOrders =
                string.Equals(
                    paymentStatus,
                    "paid",
                    StringComparison.OrdinalIgnoreCase);

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                string sql = @"
                    SELECT
                        o.OrderID,
                        o.UserID,
                        o.ReceiverName,
                        o.ReceiverPhone,
                        o.ShippingAddress,
                        o.TotalAmount,
                        o.PaymentMethod,
                        o.PaymentStatus,
                        o.OrderStatus,
                        o.CreatedAt,
                        u.FullName AS CustomerName
                    FROM dbo.Orders o
                    LEFT JOIN dbo.Users u
                        ON o.UserID = u.UserID
                    WHERE
                    (
                        @FilterPaid = 0
                        OR
                        (
                            o.PaymentStatus = N'Đã thanh toán'
                            AND ISNULL(o.OrderStatus, N'') <> N'Đã hủy'
                        )
                    )
                    ORDER BY o.CreatedAt DESC";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@FilterPaid",
                        SqlDbType.Bit).Value = filterPaidOrders;

                    connection.Open();

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            OrderListViewModel order =
                                new OrderListViewModel();

                            order.OrderID =
                                Convert.ToInt32(reader["OrderID"]);

                            order.UserID =
                                Convert.ToInt32(reader["UserID"]);

                            order.ReceiverName =
                                reader["ReceiverName"].ToString();

                            order.ReceiverPhone =
                                reader["ReceiverPhone"].ToString();

                            order.ShippingAddress =
                                reader["ShippingAddress"].ToString();

                            order.TotalAmount =
                                Convert.ToDecimal(reader["TotalAmount"]);

                            order.PaymentMethod =
                                reader["PaymentMethod"].ToString();

                            order.PaymentStatus =
                                reader["PaymentStatus"].ToString();

                            order.OrderStatus =
                                reader["OrderStatus"].ToString();

                            order.CreatedAt =
                                Convert.ToDateTime(reader["CreatedAt"]);

                            order.CustomerName =
                                reader["CustomerName"] == DBNull.Value
                                    ? ""
                                    : reader["CustomerName"].ToString();

                            orders.Add(order);
                        }
                    }
                }
            }

            ViewBag.FilterPaidOrders = filterPaidOrders;

            return View(orders);
        }

        // ================= CHI TIẾT ĐƠN HÀNG =================

        public ActionResult OrderDetails(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            Order order = null;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
                    SELECT
                        o.*,
                        u.FullName AS CustomerName
                    FROM dbo.Orders o
                    LEFT JOIN dbo.Users u
                        ON o.UserID = u.UserID
                    WHERE o.OrderID = @OrderID";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@OrderID",
                        SqlDbType.Int).Value = id;

                    using (SqlDataReader reader =
                        command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            order = new Order();

                            order.OrderID =
                                Convert.ToInt32(reader["OrderID"]);

                            order.UserID =
                                Convert.ToInt32(reader["UserID"]);

                            order.ReceiverName =
                                reader["ReceiverName"].ToString();

                            order.ReceiverPhone =
                                reader["ReceiverPhone"].ToString();

                            order.ShippingAddress =
                                reader["ShippingAddress"].ToString();

                            order.TotalAmount =
                                Convert.ToDecimal(reader["TotalAmount"]);

                            order.PaymentMethod =
                                reader["PaymentMethod"].ToString();

                            order.PaymentStatus =
                                reader["PaymentStatus"].ToString();

                            order.OrderStatus =
                                reader["OrderStatus"].ToString();

                            order.CreatedAt =
                                Convert.ToDateTime(reader["CreatedAt"]);

                            if (reader["Note"] != DBNull.Value)
                            {
                                order.Note =
                                    reader["Note"].ToString();
                            }

                            if (reader["CustomerName"] != DBNull.Value)
                            {
                                order.CustomerName =
                                    reader["CustomerName"].ToString();
                            }
                        }
                    }
                }

                if (order == null)
                {
                    return HttpNotFound();
                }

                string detailSql = @"
                    SELECT *
                    FROM dbo.OrderDetails
                    WHERE OrderID = @OrderID";

                using (SqlCommand detailCommand =
                    new SqlCommand(detailSql, connection))
                {
                    detailCommand.Parameters.Add(
                        "@OrderID",
                        SqlDbType.Int).Value = id;

                    using (SqlDataReader reader =
                        detailCommand.ExecuteReader())
                    {
                        order.OrderDetails =
                            new List<OrderDetail>();

                        while (reader.Read())
                        {
                            OrderDetail detail =
                                new OrderDetail();

                            detail.OrderDetailID =
                                Convert.ToInt32(reader["OrderDetailID"]);

                            detail.OrderID =
                                Convert.ToInt32(reader["OrderID"]);

                            detail.ProductID =
                                Convert.ToInt32(reader["ProductID"]);

                            detail.ProductName =
                                reader["ProductName"].ToString();

                            detail.Quantity =
                                Convert.ToInt32(reader["Quantity"]);

                            detail.UnitPrice =
                                Convert.ToDecimal(reader["UnitPrice"]);

                            order.OrderDetails.Add(detail);
                        }
                    }
                }
            }

            return View(order);
        }

        // ================= CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateOrderStatus(
            int orderId,
            string orderStatus)
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
                Array.IndexOf(allowedStatuses, orderStatus) < 0)
            {
                TempData["ErrorMessage"] =
                    "Trạng thái đơn hàng không hợp lệ.";

                return RedirectToAction(
                    "OrderDetails",
                    new { id = orderId });
            }

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction =
                    connection.BeginTransaction())
                {
                    try
                    {
                        string currentStatus;
                        bool stockDeducted;

                        // Khóa đơn hàng để tránh xử lý đồng thời.
                        string checkSql = @"
                            SELECT OrderStatus, StockDeducted
                            FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK)
                            WHERE OrderID = @OrderID";

                        using (SqlCommand command =
                            new SqlCommand(
                                checkSql,
                                connection,
                                transaction))
                        {
                            command.Parameters.Add(
                                "@OrderID",
                                SqlDbType.Int).Value = orderId;

                            using (SqlDataReader reader =
                                command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception(
                                        "Không tìm thấy đơn hàng.");
                                }

                                currentStatus =
                                    reader["OrderStatus"].ToString();

                                stockDeducted =
                                    reader["StockDeducted"] != DBNull.Value &&
                                    Convert.ToBoolean(reader["StockDeducted"]);
                            }
                        }

                        // Không cho phép thay đổi đơn đã kết thúc.
                        if (currentStatus == "Đã hủy" ||
                            currentStatus == "Đã giao")
                        {
                            throw new Exception(
                                "Không thể thay đổi trạng thái đơn hàng đã kết thúc.");
                        }

                        // =============================================
                        // HỦY ĐƠN VÀ HOÀN LẠI TỒN KHO
                        // =============================================

                        if (orderStatus == "Đã hủy" && stockDeducted)
                        {
                            string restoreSql = @"
                                UPDATE p
                                SET p.StockQuantity =
                                    p.StockQuantity + d.TotalQuantity
                                FROM dbo.Products p
                                INNER JOIN
                                (
                                    SELECT
                                        ProductID,
                                        SUM(Quantity) AS TotalQuantity
                                    FROM dbo.OrderDetails
                                    WHERE OrderID = @OrderID
                                    GROUP BY ProductID
                                ) d
                                    ON p.ProductID = d.ProductID";

                            using (SqlCommand restoreCommand =
                                new SqlCommand(
                                    restoreSql,
                                    connection,
                                    transaction))
                            {
                                restoreCommand.Parameters.Add(
                                    "@OrderID",
                                    SqlDbType.Int).Value = orderId;

                                restoreCommand.ExecuteNonQuery();
                            }

                            // Đã hoàn kho, không được hoàn lần thứ hai.
                            stockDeducted = false;
                        }

                        // =============================================
                        // CẬP NHẬT TRẠNG THÁI ĐƠN HÀNG
                        // Không trừ kho khi Đã giao.
                        // =============================================

                        string updateSql = @"
                            UPDATE dbo.Orders
                            SET
                                OrderStatus = @OrderStatus,
                                StockDeducted = @StockDeducted
                            WHERE OrderID = @OrderID";

                        using (SqlCommand updateCommand =
                            new SqlCommand(
                                updateSql,
                                connection,
                                transaction))
                        {
                            updateCommand.Parameters.Add(
                                "@OrderStatus",
                                SqlDbType.NVarChar).Value = orderStatus;

                            updateCommand.Parameters.Add(
                                "@StockDeducted",
                                SqlDbType.Bit).Value = stockDeducted;

                            updateCommand.Parameters.Add(
                                "@OrderID",
                                SqlDbType.Int).Value = orderId;

                            int affectedRows =
                                updateCommand.ExecuteNonQuery();

                            if (affectedRows != 1)
                            {
                                throw new Exception(
                                    "Không thể cập nhật trạng thái đơn hàng.");
                            }
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] =
                            orderStatus == "Đã hủy" && !stockDeducted
                                ? "Cập nhật trạng thái đơn hàng thành công!"
                                : "Cập nhật trạng thái đơn hàng thành công!";
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                            // Bỏ qua lỗi rollback phụ.
                        }

                        TempData["ErrorMessage"] = ex.Message;
                    }
                }
            }

            return RedirectToAction(
                "OrderDetails",
                new { id = orderId });
        }

        // ================= ADMIN XÁC NHẬN THANH TOÁN =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ConfirmPayment(int orderId)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            {
                connection.Open();

                string sql = @"
                    UPDATE dbo.Orders
                    SET PaymentStatus = N'Đã thanh toán'
                    WHERE OrderID = @OrderID
                      AND PaymentStatus = N'Chưa thanh toán'
                      AND OrderStatus <> N'Đã hủy'";

                using (SqlCommand command =
                    new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(
                        "@OrderID",
                        SqlDbType.Int).Value = orderId;

                    int rowsAffected =
                        command.ExecuteNonQuery();

                    if (rowsAffected > 0)
                    {
                        TempData["SuccessMessage"] =
                            "Xác nhận thanh toán thành công!";
                    }
                    else
                    {
                        string checkSql = @"
                            SELECT PaymentStatus, OrderStatus
                            FROM dbo.Orders
                            WHERE OrderID = @OrderID";

                        using (SqlCommand checkCommand =
                            new SqlCommand(checkSql, connection))
                        {
                            checkCommand.Parameters.Add(
                                "@OrderID",
                                SqlDbType.Int).Value = orderId;

                            using (SqlDataReader reader =
                                checkCommand.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    TempData["ErrorMessage"] =
                                        "Không tìm thấy đơn hàng.";
                                }
                                else if (
                                    reader["OrderStatus"].ToString()
                                    == "Đã hủy")
                                {
                                    TempData["ErrorMessage"] =
                                        "Không thể xác nhận thanh toán cho đơn hàng đã hủy.";
                                }
                                else if (
                                    reader["PaymentStatus"].ToString()
                                    == "Đã thanh toán")
                                {
                                    TempData["ErrorMessage"] =
                                        "Đơn hàng này đã được thanh toán trước đó.";
                                }
                                else
                                {
                                    TempData["ErrorMessage"] =
                                        "Không thể cập nhật trạng thái thanh toán.";
                                }
                            }
                        }
                    }
                }
            }

            return RedirectToAction(
                "OrderDetails",
                new { id = orderId });
        }
    }
}
