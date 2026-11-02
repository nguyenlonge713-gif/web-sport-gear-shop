
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using web_sport_gear.Models;

namespace web_sport_gear.Controllers
{
    public class CheckoutController : Controller
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"].ConnectionString;
            }
        }

        // Hiển thị trang thanh toán
        [HttpGet]
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new CheckoutViewModel
            {
                ReceiverName = Session["FullName"]?.ToString()
            };

            return View(model);
        }

        // Tiếp nhận và lưu đơn hàng COD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(CheckoutViewModel model, string cartJson)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Đọc giỏ hàng được gửi từ trình duyệt
            List<CheckoutCartItem> cart;

            try
            {
                var serializer = new JavaScriptSerializer();

                cart = serializer.Deserialize<List<CheckoutCartItem>>(cartJson);
            }
            catch
            {
                ModelState.AddModelError("", "Dữ liệu giỏ hàng không hợp lệ.");
                return View(model);
            }

            if (cart == null || cart.Count == 0)
            {
                ModelState.AddModelError("", "Giỏ hàng đang trống.");
                return View(model);
            }

            // Kiểm tra và gộp các sản phẩm bị trùng mã
            var groupedCart = cart
                .GroupBy(x => x.Id)
                .Select(g => new CheckoutCartItem
                {
                    Id = g.Key,
                    Quantity = g.Sum(x => x.Quantity)
                })
                .ToList();

            if (groupedCart.Any(x => x.Id <= 0 || x.Quantity <= 0))
            {
                ModelState.AddModelError("", "Số lượng hoặc mã sản phẩm không hợp lệ.");
                return View(model);
            }

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        decimal totalAmount = 0;

                        var orderDetails = new List<OrderDetail>();

                        // Lấy giá và trạng thái sản phẩm trực tiếp từ SQL Server
                        foreach (var item in groupedCart)
                        {
                            string productSql = @"
                                SELECT ProductID, ProductName, Price
                                FROM dbo.Products
                                WHERE ProductID = @ProductID
                                  AND IsActive = 1";

                            using (SqlCommand command = new SqlCommand(
                                productSql, connection, transaction))
                            {
                                command.Parameters.Add("@ProductID", SqlDbType.Int)
                                    .Value = item.Id;

                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new Exception(
                                            "Một sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
                                    }

                                    var detail = new OrderDetail
                                    {
                                        ProductID = Convert.ToInt32(reader["ProductID"]),
                                        ProductName = reader["ProductName"].ToString(),
                                        Quantity = item.Quantity,
                                        UnitPrice = Convert.ToDecimal(reader["Price"])
                                    };

                                    orderDetails.Add(detail);
                                    totalAmount += detail.SubTotal;
                                }
                            }
                            // Kiểm tra và trừ tồn kho an toàn
                            string stockSql = @"
    UPDATE dbo.Products
    SET StockQuantity = StockQuantity - @Quantity
    WHERE ProductID = @ProductID
      AND IsActive = 1
      AND StockQuantity >= @Quantity";

                            using (SqlCommand stockCommand = new SqlCommand(
                                stockSql, connection, transaction))
                            {
                                stockCommand.Parameters.Add("@ProductID", SqlDbType.Int)
                                    .Value = item.Id;

                                stockCommand.Parameters.Add("@Quantity", SqlDbType.Int)
                                    .Value = item.Quantity;

                                int stockRowsAffected = stockCommand.ExecuteNonQuery();

                                if (stockRowsAffected == 0)
                                {
                                    throw new Exception(
                                        "Sản phẩm " + orderDetails.Last().ProductName +
                                        " không đủ số lượng tồn kho. Vui lòng kiểm tra lại giỏ hàng.");
                                }
                            }
                        }

                        // Tạo đơn hàng
                        string orderSql = @"
                            INSERT INTO dbo.Orders
                            (
                                UserID,
                                ReceiverName,
                                ReceiverPhone,
                                ShippingAddress,
                                TotalAmount,
                                PaymentMethod,
                                PaymentStatus,
                                OrderStatus,
                                Note
                            )
                            OUTPUT INSERTED.OrderID
                            VALUES
                            (
                                @UserID,
                                @ReceiverName,
                                @ReceiverPhone,
                                @ShippingAddress,
                                @TotalAmount,
                                N'COD',
                                N'Chưa thanh toán',
                                N'Chờ xác nhận',
                                @Note
                            )";

                        int orderId;

                        using (SqlCommand command = new SqlCommand(
                            orderSql, connection, transaction))
                        {
                            command.Parameters.Add("@UserID", SqlDbType.Int)
                                .Value = userId;

                            command.Parameters.Add("@ReceiverName", SqlDbType.NVarChar, 100)
                                .Value = model.ReceiverName.Trim();

                            command.Parameters.Add("@ReceiverPhone", SqlDbType.NVarChar, 20)
                                .Value = model.ReceiverPhone.Trim();

                            command.Parameters.Add("@ShippingAddress", SqlDbType.NVarChar, 500)
                                .Value = model.ShippingAddress.Trim();

                            SqlParameter totalParameter =
                                command.Parameters.Add("@TotalAmount", SqlDbType.Decimal);

                            totalParameter.Precision = 18;
                            totalParameter.Scale = 2;
                            totalParameter.Value = totalAmount;

                            command.Parameters.Add("@Note", SqlDbType.NVarChar, 500)
                                .Value = string.IsNullOrWhiteSpace(model.Note)
                                    ? (object)DBNull.Value
                                    : model.Note.Trim();

                            orderId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // Lưu từng sản phẩm vào OrderDetails
                        string detailSql = @"
                            INSERT INTO dbo.OrderDetails
                            (
                                OrderID,
                                ProductID,
                                ProductName,
                                Quantity,
                                UnitPrice
                            )
                            VALUES
                            (
                                @OrderID,
                                @ProductID,
                                @ProductName,
                                @Quantity,
                                @UnitPrice
                            )";

                        foreach (var detail in orderDetails)
                        {
                            using (SqlCommand command = new SqlCommand(
                                detailSql, connection, transaction))
                            {
                                command.Parameters.Add("@OrderID", SqlDbType.Int)
                                    .Value = orderId;

                                command.Parameters.Add("@ProductID", SqlDbType.Int)
                                    .Value = detail.ProductID;

                                command.Parameters.Add("@ProductName", SqlDbType.NVarChar, 200)
                                    .Value = detail.ProductName;

                                command.Parameters.Add("@Quantity", SqlDbType.Int)
                                    .Value = detail.Quantity;

                                SqlParameter priceParameter =
                                    command.Parameters.Add("@UnitPrice", SqlDbType.Decimal);

                                priceParameter.Precision = 18;
                                priceParameter.Scale = 2;
                                priceParameter.Value = detail.UnitPrice;

                                command.ExecuteNonQuery();
                            }
                        }

                        // Chỉ xác nhận khi cả đơn hàng và chi tiết đều lưu thành công
                        transaction.Commit();

                        TempData["OrderSuccess"] =
                            "Đặt hàng thành công! Mã đơn hàng: " + orderId;

                        return RedirectToAction("Success");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();

                        ModelState.AddModelError(
                            "",
                            "Không thể đặt hàng: " + ex.Message);

                        return View(model);
                    }
                }
            }
        }

        // Trang thông báo đặt hàng thành công
        [HttpGet]
        public ActionResult Success()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (TempData["OrderSuccess"] == null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = TempData["OrderSuccess"];

            return View();
        }
    }
}
