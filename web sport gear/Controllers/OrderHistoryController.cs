
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Controllers
{
    public class OrderHistoryController : Controller
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"].ConnectionString;
            }
        }

        // GET: OrderHistory
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            List<OrderHistoryViewModel> orders =
                new List<OrderHistoryViewModel>();

            string sql = @"
                SELECT
                    OrderID,
                    TotalAmount,
                    PaymentMethod,
                    PaymentStatus,
                    OrderStatus,
                    CreatedAt
                FROM dbo.Orders
                WHERE UserID = @UserID
                ORDER BY CreatedAt DESC";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@UserID", userId);

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        orders.Add(new OrderHistoryViewModel
                        {
                            OrderID = Convert.ToInt32(reader["OrderID"]),
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

        // GET: OrderHistory/Details/1
        public ActionResult Details(int id)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            Order order = null;

            // Chỉ lấy đơn hàng thuộc tài khoản đang đăng nhập
            string sqlOrder = @"
                SELECT *
                FROM dbo.Orders
                WHERE OrderID = @OrderID
                  AND UserID = @UserID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sqlOrder, connection))
            {
                command.Parameters.AddWithValue("@OrderID", id);
                command.Parameters.AddWithValue("@UserID", userId);

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
                    }
                }
            }

            // Không tìm thấy hoặc đơn hàng không thuộc tài khoản này
            if (order == null)
            {
                return HttpNotFound();
            }

            // Lấy danh sách sản phẩm trong đơn hàng
            order.OrderDetails = new List<OrderDetail>();

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

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sqlDetails, connection))
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
    }
}
