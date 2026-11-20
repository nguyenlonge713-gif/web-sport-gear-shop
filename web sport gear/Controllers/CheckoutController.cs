
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
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
                ConnectionStringSettings settings =
                    ConfigurationManager.ConnectionStrings["GearSportDB"];

                if (settings == null ||
                    string.IsNullOrWhiteSpace(settings.ConnectionString))
                {
                    throw new Exception(
                        "Không tìm thấy chuỗi kết nối GearSportDB.");
                }

                return settings.ConnectionString;
            }
        }

        // ================= HIỂN THỊ TRANG THANH TOÁN =================

        [HttpGet]
        public ActionResult Index()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId;

            if (!int.TryParse(Session["UserID"].ToString(), out userId) ||
                userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var model = new CheckoutViewModel();

            string sql = @"
                SELECT FullName, Phone, Address
                FROM dbo.Users
                WHERE UserID = @UserID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@UserID", SqlDbType.Int).Value = userId;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    model.ReceiverName =
                        reader["FullName"] == DBNull.Value
                            ? ""
                            : reader["FullName"].ToString().Trim();

                    model.ReceiverPhone =
                        reader["Phone"] == DBNull.Value
                            ? ""
                            : reader["Phone"].ToString().Trim();

                    model.ShippingAddress =
                        reader["Address"] == DBNull.Value
                            ? ""
                            : reader["Address"].ToString().Trim();
                }
            }

            return View(model);
        }

        // ================= KIỂM TRA SỐ ĐIỆN THOẠI =================

        private bool IsValidPhone(string phone, out string error)
        {
            error = "";

            if (string.IsNullOrWhiteSpace(phone))
            {
                error = "Vui lòng nhập số điện thoại.";
                return false;
            }

            if (!Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            {
                error =
                    "Số điện thoại phải gồm đúng 10 chữ số, bắt đầu bằng số 0.";

                return false;
            }

            if (phone.All(c => c == phone[0]))
            {
                error =
                    "Số điện thoại không được chứa toàn bộ chữ số giống nhau.";

                return false;
            }

            return true;
        }

        // ================= KIỂM TRA THÔNG TIN GIAO HÀNG =================

        private void ValidateShippingInformation(
            CheckoutViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.ReceiverName))
            {
                ModelState.AddModelError(
                    "ReceiverName",
                    "Vui lòng nhập họ tên người nhận.");
            }
            else if (model.ReceiverName.Length > 100)
            {
                ModelState.AddModelError(
                    "ReceiverName",
                    "Họ tên người nhận không được vượt quá 100 ký tự.");
            }

            string phoneError;

            if (!IsValidPhone(model.ReceiverPhone, out phoneError))
            {
                ModelState.AddModelError(
                    "ReceiverPhone",
                    phoneError);
            }

            if (string.IsNullOrWhiteSpace(model.ShippingAddress))
            {
                ModelState.AddModelError(
                    "ShippingAddress",
                    "Vui lòng nhập địa chỉ giao hàng.");
            }
            else if (model.ShippingAddress.Length < 10)
            {
                ModelState.AddModelError(
                    "ShippingAddress",
                    "Địa chỉ giao hàng phải có ít nhất 10 ký tự.");
            }
            else if (model.ShippingAddress.Length > 300)
            {
                ModelState.AddModelError(
                    "ShippingAddress",
                    "Địa chỉ giao hàng không được vượt quá 300 ký tự.");
            }

            if (model.Note != null && model.Note.Length > 500)
            {
                ModelState.AddModelError(
                    "Note",
                    "Ghi chú không được vượt quá 500 ký tự.");
            }
        }

        // ================= ROLLBACK AN TOÀN =================

        private void RollbackSafely(SqlTransaction transaction)
        {
            if (transaction == null)
                return;

            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT ROLLBACK ERROR: " + ex);
            }
        }

        // ================= TIẾP NHẬN VÀ LƯU ĐƠN HÀNG COD =================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(
            CheckoutViewModel model,
            string cartJson)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (model == null)
            {
                return RedirectToAction("Index");
            }

            int userId;

            if (!int.TryParse(Session["UserID"].ToString(), out userId) ||
                userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Chỉ tài khoản khách hàng được đặt hàng
            if (Session["Role"] == null ||
                !string.Equals(
                    Session["Role"].ToString(),
                    "Customer",
                    StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "Home");
            }

            // Chuẩn hóa dữ liệu
            model.ReceiverName = model.ReceiverName == null
                ? null
                : model.ReceiverName.Trim();

            model.ReceiverPhone = model.ReceiverPhone == null
                ? null
                : model.ReceiverPhone.Trim();

            model.ShippingAddress = model.ShippingAddress == null
                ? null
                : model.ShippingAddress.Trim();

            model.Note = model.Note == null
                ? null
                : model.Note.Trim();

            // Kiểm tra thông tin giao hàng
            ValidateShippingInformation(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ================= ĐỌC GIỎ HÀNG =================

            List<CheckoutCartItem> cart;

            try
            {
                if (string.IsNullOrWhiteSpace(cartJson))
                {
                    throw new Exception("Dữ liệu giỏ hàng đang rỗng.");
                }

                JavaScriptSerializer serializer =
                    new JavaScriptSerializer();

                cart = serializer.Deserialize<List<CheckoutCartItem>>(
                    cartJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT DESERIALIZE ERROR: " + ex);

                ModelState.AddModelError(
                    "",
                    "Dữ liệu giỏ hàng không hợp lệ. Vui lòng tải lại trang.");

                return View(model);
            }

            if (cart == null || cart.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Giỏ hàng đang trống.");

                return View(model);
            }

            if (cart.Count > 100 || cart.Any(x => x == null))
            {
                ModelState.AddModelError(
                    "",
                    "Dữ liệu giỏ hàng không hợp lệ.");

                return View(model);
            }

            // ================= GỘP SẢN PHẨM TRÙNG MÃ =================

            List<CheckoutCartItem> groupedCart;

            try
            {
                groupedCart = cart
                    .GroupBy(x => x.Id)
                    .Select(g => new CheckoutCartItem
                    {
                        Id = g.Key,
                        Quantity = g.Aggregate(
                            0,
                            (total, item) =>
                                checked(total + item.Quantity))
                    })
                    .OrderBy(x => x.Id)
                    .ToList();
            }
            catch (OverflowException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT GROUP CART ERROR: " + ex);

                ModelState.AddModelError(
                    "",
                    "Số lượng sản phẩm vượt quá giới hạn.");

                return View(model);
            }

            if (groupedCart.Any(
                x => x.Id <= 0 || x.Quantity <= 0))
            {
                ModelState.AddModelError(
                    "",
                    "Mã sản phẩm hoặc số lượng không hợp lệ.");

                return View(model);
            }

            // ================= BẮT ĐẦU TRANSACTION =================

            SqlTransaction transaction = null;

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    transaction = connection.BeginTransaction();

                    try
                    {
                        decimal totalAmount = 0;

                        List<OrderDetail> orderDetails =
                            new List<OrderDetail>();

                        // =================================================
                        // BƯỚC 1: LẤY DANH SÁCH SẢN PHẨM ĐANG ĐƯỢC GIỮ
                        // Không còn kiểm tra thời hạn giữ hàng.
                        // =================================================

                        Dictionary<int, int> reservedCart =
                            new Dictionary<int, int>();

                        string reservationSql = @"
                            SELECT
                                ProductID,
                                Quantity
                            FROM dbo.CartReservations WITH (UPDLOCK, HOLDLOCK)
                            WHERE UserID = @UserID
                            ORDER BY ProductID";

                        using (SqlCommand command = new SqlCommand(
                            reservationSql,
                            connection,
                            transaction))
                        {
                            command.Parameters.Add(
                                "@UserID",
                                SqlDbType.Int).Value = userId;

                            using (SqlDataReader reader =
                                command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int productId =
                                        Convert.ToInt32(
                                            reader["ProductID"]);

                                    int reservedQuantity =
                                        Convert.ToInt32(
                                            reader["Quantity"]);

                                    reservedCart.Add(
                                        productId,
                                        reservedQuantity);
                                }
                            }
                        }

                        // =================================================
                        // BƯỚC 2: ĐỐI CHIẾU GIỎ HÀNG VỚI DỮ LIỆU SQL
                        // =================================================

                        if (reservedCart.Count != groupedCart.Count)
                        {
                            throw new CheckoutException(
                                "Giỏ hàng đã thay đổi hoặc không còn hợp lệ. " +
                                "Vui lòng tải lại giỏ hàng.");
                        }

                        foreach (CheckoutCartItem item in groupedCart)
                        {
                            int reservedQuantity;

                            if (!reservedCart.TryGetValue(
                                item.Id,
                                out reservedQuantity))
                            {
                                throw new CheckoutException(
                                    "Sản phẩm ID " + item.Id +
                                    " chưa được giữ tồn kho. " +
                                    "Vui lòng quay lại giỏ hàng.");
                            }

                            if (reservedQuantity != item.Quantity)
                            {
                                throw new CheckoutException(
                                    "Số lượng sản phẩm ID " + item.Id +
                                    " không khớp với dữ liệu hệ thống. " +
                                    "Vui lòng tải lại giỏ hàng.");
                            }
                        }

                        // =================================================
                        // BƯỚC 3: KIỂM TRA SẢN PHẨM VÀ LẤY GIÁ TỪ DATABASE
                        // =================================================

                        foreach (CheckoutCartItem item in groupedCart)
                        {
                            string productSql = @"
                                SELECT
                                    ProductID,
                                    ProductName,
                                    Price
                                FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
                                WHERE ProductID = @ProductID
                                  AND IsActive = 1";

                            OrderDetail detail;

                            using (SqlCommand command = new SqlCommand(
                                productSql,
                                connection,
                                transaction))
                            {
                                command.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int).Value = item.Id;

                                using (SqlDataReader reader =
                                    command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new CheckoutException(
                                            "Sản phẩm ID " + item.Id +
                                            " không tồn tại hoặc đã ngừng kinh doanh.");
                                    }

                                    int databaseProductId =
                                        Convert.ToInt32(
                                            reader["ProductID"]);

                                    string productName =
                                        reader["ProductName"].ToString();

                                    decimal unitPrice =
                                        Convert.ToDecimal(reader["Price"]);

                                    if (unitPrice <= 0)
                                    {
                                        throw new CheckoutException(
                                            "Giá sản phẩm " + productName +
                                            " không hợp lệ.");
                                    }

                                    detail = new OrderDetail
                                    {
                                        ProductID = databaseProductId,
                                        ProductName = productName,
                                        Quantity = item.Quantity,
                                        UnitPrice = decimal.Round(
                                            unitPrice,
                                            2,
                                            MidpointRounding.AwayFromZero)
                                    };
                                }
                            }

                            orderDetails.Add(detail);

                            totalAmount +=
                                detail.UnitPrice * detail.Quantity;
                        }

                        // =================================================
                        // BƯỚC 4: KIỂM TRA TỔNG TIỀN
                        // =================================================

                        if (totalAmount <= 0)
                        {
                            throw new CheckoutException(
                                "Tổng giá trị đơn hàng không hợp lệ.");
                        }

                        // =================================================
                        // BƯỚC 5: TẠO ĐƠN HÀNG
                        // StockDeducted = 1 vì tồn kho đã trừ khi thêm giỏ.
                        // Không thực hiện UPDATE Products ở bước này.
                        // =================================================

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
                                Note,
                                StockDeducted
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
                                @Note,
                                1
                            )";

                        int orderId;

                        using (SqlCommand command = new SqlCommand(
                            orderSql,
                            connection,
                            transaction))
                        {
                            command.Parameters.Add(
                                "@UserID",
                                SqlDbType.Int).Value = userId;

                            command.Parameters.Add(
                                "@ReceiverName",
                                SqlDbType.NVarChar,
                                100).Value = model.ReceiverName;

                            command.Parameters.Add(
                                "@ReceiverPhone",
                                SqlDbType.NVarChar,
                                10).Value = model.ReceiverPhone;

                            command.Parameters.Add(
                                "@ShippingAddress",
                                SqlDbType.NVarChar,
                                300).Value = model.ShippingAddress;

                            SqlParameter totalParameter =
                                command.Parameters.Add(
                                    "@TotalAmount",
                                    SqlDbType.Decimal);

                            totalParameter.Precision = 18;
                            totalParameter.Scale = 2;
                            totalParameter.Value = totalAmount;

                            command.Parameters.Add(
                                "@Note",
                                SqlDbType.NVarChar,
                                500).Value =
                                string.IsNullOrWhiteSpace(model.Note)
                                    ? (object)DBNull.Value
                                    : model.Note;

                            orderId = Convert.ToInt32(
                                command.ExecuteScalar());
                        }

                        // =================================================
                        // BƯỚC 6: LƯU CHI TIẾT ĐƠN HÀNG
                        // =================================================

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

                        foreach (OrderDetail detail in orderDetails)
                        {
                            using (SqlCommand command = new SqlCommand(
                                detailSql,
                                connection,
                                transaction))
                            {
                                command.Parameters.Add(
                                    "@OrderID",
                                    SqlDbType.Int).Value = orderId;

                                command.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int).Value =
                                    detail.ProductID;

                                command.Parameters.Add(
                                    "@ProductName",
                                    SqlDbType.NVarChar,
                                    400).Value = detail.ProductName;

                                command.Parameters.Add(
                                    "@Quantity",
                                    SqlDbType.Int).Value =
                                    detail.Quantity;

                                SqlParameter priceParameter =
                                    command.Parameters.Add(
                                        "@UnitPrice",
                                        SqlDbType.Decimal);

                                priceParameter.Precision = 18;
                                priceParameter.Scale = 2;
                                priceParameter.Value = detail.UnitPrice;

                                int affectedRows =
                                    command.ExecuteNonQuery();

                                if (affectedRows != 1)
                                {
                                    throw new CheckoutException(
                                        "Không thể lưu chi tiết đơn hàng.");
                                }
                            }
                        }

                        // =================================================
                        // BƯỚC 7: XÓA DỮ LIỆU GIỮ HÀNG TRONG GIỎ
                        // Không hoàn kho vì hàng đã chuyển sang đơn hàng.
                        // =================================================

                        string deleteReservationSql = @"
                            DELETE FROM dbo.CartReservations
                            WHERE UserID = @UserID";

                        using (SqlCommand command = new SqlCommand(
                            deleteReservationSql,
                            connection,
                            transaction))
                        {
                            command.Parameters.Add(
                                "@UserID",
                                SqlDbType.Int).Value = userId;

                            int deletedRows =
                                command.ExecuteNonQuery();

                            if (deletedRows != groupedCart.Count)
                            {
                                throw new CheckoutException(
                                    "Không thể hoàn tất việc xác nhận giữ hàng. " +
                                    "Vui lòng thử lại.");
                            }
                        }

                        // =================================================
                        // BƯỚC 8: HOÀN TẤT GIAO DỊCH
                        // =================================================

                        transaction.Commit();

                        TempData["OrderSuccess"] =
                            "Đặt hàng thành công! Mã đơn hàng: " + orderId;

                        TempData["OrderId"] = orderId;

                        return RedirectToAction("Success");
                    }
                    catch (CheckoutException ex)
                    {
                        RollbackSafely(transaction);

                        ModelState.AddModelError("", ex.Message);

                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "CHECKOUT ERROR: " + ex);

                        RollbackSafely(transaction);

                        ModelState.AddModelError(
                            "",
                            "Đã xảy ra lỗi khi lưu đơn hàng. " +
                            "Vui lòng thử lại sau.");

                        return View(model);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT CONNECTION ERROR: " + ex);

                RollbackSafely(transaction);

                ModelState.AddModelError(
                    "",
                    "Không thể kết nối hoặc xử lý dữ liệu đơn hàng. " +
                    "Vui lòng thử lại sau.");

                return View(model);
            }
            finally
            {
                if (transaction != null)
                {
                    transaction.Dispose();
                }
            }
        }

        // ================= TRANG ĐẶT HÀNG THÀNH CÔNG =================

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
            ViewBag.OrderId = TempData["OrderId"];

            return View();
        }

        // ================= EXCEPTION LỖI NGHIỆP VỤ =================

        private class CheckoutException : Exception
        {
            public CheckoutException(string message)
                : base(message)
            {
            }
        }
    }
}
