
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
            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy UserID từ Session
            int userId;

            if (!int.TryParse(Session["UserID"].ToString(), out userId) ||
                userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            // Khởi tạo ViewModel
            var model = new CheckoutViewModel();

            // Lấy thông tin khách hàng từ database
            string sql = @"
        SELECT
            FullName,
            Phone,
            Address
        FROM dbo.Users
        WHERE UserID = @UserID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@UserID",
                    SqlDbType.Int
                ).Value = userId;

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

            // Không tự ý loại bỏ dấu cách hoặc ký tự đặc biệt.
            // Người dùng phải nhập đúng định dạng.
            if (!Regex.IsMatch(phone, @"^0[0-9]{9}$"))
            {
                error =
                    "Số điện thoại phải gồm đúng 10 chữ số, bắt đầu bằng số 0.";

                return false;
            }

            // Không chấp nhận 10 chữ số giống nhau.
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
            // Họ tên người nhận
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

            // Số điện thoại
            string phoneError;

            if (!IsValidPhone(model.ReceiverPhone, out phoneError))
            {
                ModelState.AddModelError(
                    "ReceiverPhone",
                    phoneError);
            }

            // Địa chỉ giao hàng
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

            // Ghi chú: không bắt buộc, tối đa 500 ký tự
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
            {
                return;
            }

            try
            {
                transaction.Rollback();

                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT: Transaction đã rollback.");
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
            System.Diagnostics.Debug.WriteLine("");

            System.Diagnostics.Debug.WriteLine(
                "========== CHECKOUT DEBUG START ==========");

            System.Diagnostics.Debug.WriteLine(
                "cartJson nhận được: " + cartJson);

            // Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT: Người dùng chưa đăng nhập.");

                return RedirectToAction("Login", "Account");
            }

            if (model == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT ERROR: Model bị null.");

                return RedirectToAction("Index");
            }

            // Lấy UserID từ Session
            int userId;

            if (!int.TryParse(
                Session["UserID"].ToString(),
                out userId) ||
                userId <= 0)
            {
                return RedirectToAction("Login", "Account");
            }

            System.Diagnostics.Debug.WriteLine(
                "UserID: " + userId);

            // ================= CHUẨN HÓA DỮ LIỆU =================

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

            // ================= KIỂM TRA THÔNG TIN GIAO HÀNG =================

            ValidateShippingInformation(model);

            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine(
                    "CHECKOUT ERROR: ModelState không hợp lệ.");

                foreach (var error in ModelState)
                {
                    foreach (var item in error.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            error.Key + ": " + item.ErrorMessage);
                    }
                }

                return View(model);
            }

            // ================= ĐỌC GIỎ HÀNG =================

            List<CheckoutCartItem> cart;

            try
            {
                if (string.IsNullOrWhiteSpace(cartJson))
                {
                    throw new Exception("cartJson đang rỗng.");
                }

                JavaScriptSerializer serializer =
                    new JavaScriptSerializer();

                cart = serializer.Deserialize<List<CheckoutCartItem>>(
                    cartJson);

                System.Diagnostics.Debug.WriteLine(
                    "========== DESERIALIZE CART ==========");

                if (cart != null)
                {
                    foreach (var item in cart)
                    {
                        if (item != null)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "ProductID: " + item.Id +
                                " | Quantity: " + item.Quantity);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "DESERIALIZE ERROR: " + ex);

                ModelState.AddModelError(
                    "",
                    "Dữ liệu giỏ hàng không hợp lệ. Vui lòng tải lại trang.");

                return View(model);
            }

            // Kiểm tra giỏ hàng rỗng
            if (cart == null || cart.Count == 0)
            {
                ModelState.AddModelError(
                    "",
                    "Giỏ hàng đang trống.");

                return View(model);
            }

            // Giới hạn số lượng phần tử và kiểm tra phần tử null
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
                    "GROUP CART ERROR: " + ex);

                ModelState.AddModelError(
                    "",
                    "Số lượng sản phẩm vượt quá giới hạn.");

                return View(model);
            }

            System.Diagnostics.Debug.WriteLine(
                "========== GROUPED CART ==========");

            foreach (var item in groupedCart)
            {
                System.Diagnostics.Debug.WriteLine(
                    "ProductID: " + item.Id +
                    " | Quantity: " + item.Quantity);
            }

            // Kiểm tra mã sản phẩm và số lượng
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

                    System.Diagnostics.Debug.WriteLine(
                        "CHECKOUT: Đã kết nối database.");

                    transaction = connection.BeginTransaction();

                    try
                    {
                        decimal totalAmount = 0;

                        List<OrderDetail> orderDetails =
                            new List<OrderDetail>();

                        // ================= KIỂM TRA SẢN PHẨM =================
                        // Không trừ tồn kho tại bước Checkout.

                        foreach (CheckoutCartItem item in groupedCart)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                "========== QUERY PRODUCT ==========");

                            System.Diagnostics.Debug.WriteLine(
                                "ProductID gửi vào SQL: " + item.Id);

                            string productSql = @"
                                SELECT
                                    ProductID,
                                    ProductName,
                                    Price,
                                    StockQuantity
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

                                    int stockQuantity =
                                        Convert.ToInt32(
                                            reader["StockQuantity"]);

                                    string productName =
                                        reader["ProductName"].ToString();

                                    int databaseProductId =
                                        Convert.ToInt32(
                                            reader["ProductID"]);

                                    System.Diagnostics.Debug.WriteLine(
                                        "========== PRODUCT FROM DATABASE ==========");

                                    System.Diagnostics.Debug.WriteLine(
                                        "ProductID: " + databaseProductId);

                                    System.Diagnostics.Debug.WriteLine(
                                        "ProductName: " + productName);

                                    System.Diagnostics.Debug.WriteLine(
                                        "StockQuantity: " + stockQuantity);

                                    // Kiểm tra tồn kho
                                    if (stockQuantity < item.Quantity)
                                    {
                                        throw new CheckoutException(
                                            "Sản phẩm " + productName +
                                            " không đủ số lượng tồn kho. " +
                                            "Số lượng còn lại: " +
                                            stockQuantity +
                                            ". Vui lòng kiểm tra lại giỏ hàng.");
                                    }

                                    detail = new OrderDetail
                                    {
                                        ProductID = databaseProductId,

                                        ProductName = productName,

                                        Quantity = item.Quantity,

                                        UnitPrice = decimal.Round(
                                            Convert.ToDecimal(
                                                reader["Price"]),
                                            2,
                                            MidpointRounding.AwayFromZero)
                                    };
                                }
                            }

                            orderDetails.Add(detail);

                            System.Diagnostics.Debug.WriteLine(
                                "========== ORDER DETAIL CREATED ==========");

                            System.Diagnostics.Debug.WriteLine(
                                "ProductID: " + detail.ProductID +
                                " | ProductName: " + detail.ProductName +
                                " | Quantity: " + detail.Quantity +
                                " | UnitPrice: " + detail.UnitPrice);

                            // Tính tổng tiền theo database
                            totalAmount +=
                                detail.UnitPrice * detail.Quantity;
                        }

                        // ================= KIỂM TRA TỔNG TIỀN =================

                        if (totalAmount <= 0)
                        {
                            throw new CheckoutException(
                                "Tổng giá trị đơn hàng không hợp lệ.");
                        }

                        System.Diagnostics.Debug.WriteLine(
                            "========== ORDER TOTAL ==========");

                        System.Diagnostics.Debug.WriteLine(
                            "TotalAmount: " + totalAmount);

                        // ================= TẠO ĐƠN HÀNG =================

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

                        System.Diagnostics.Debug.WriteLine(
                            "========== ORDER CREATED ==========");

                        System.Diagnostics.Debug.WriteLine(
                            "New OrderID: " + orderId);

                        // ================= LƯU CHI TIẾT ĐƠN HÀNG =================

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
                            System.Diagnostics.Debug.WriteLine(
                                "========== INSERT ORDER DETAIL ==========");

                            System.Diagnostics.Debug.WriteLine(
                                "OrderID: " + orderId +
                                " | ProductID: " + detail.ProductID +
                                " | ProductName: " + detail.ProductName +
                                " | Quantity: " + detail.Quantity +
                                " | UnitPrice: " + detail.UnitPrice);

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

                                System.Diagnostics.Debug.WriteLine(
                                    "Rows inserted: " + affectedRows);
                            }
                        }

                        // ================= HOÀN TẤT TRANSACTION =================

                        transaction.Commit();

                        System.Diagnostics.Debug.WriteLine(
                            "========== CHECKOUT COMMIT SUCCESS ==========");

                        System.Diagnostics.Debug.WriteLine(
                            "OrderID: " + orderId);

                        System.Diagnostics.Debug.WriteLine(
                            "TotalAmount: " + totalAmount);

                        System.Diagnostics.Debug.WriteLine(
                            "========== CHECKOUT DEBUG END ==========");

                        TempData["OrderSuccess"] =
                            "Đặt hàng thành công! Mã đơn hàng: " + orderId;

                        TempData["OrderId"] = orderId;

                        return RedirectToAction("Success");
                    }
                    catch (CheckoutException ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "CHECKOUT BUSINESS ERROR: " + ex);

                        RollbackSafely(transaction);

                        ModelState.AddModelError("", ex.Message);

                        return View(model);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(
                            "========== CHECKOUT SQL/C# ERROR ==========");

                        System.Diagnostics.Debug.WriteLine(
                            ex.ToString());

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
                    "========== CHECKOUT CONNECTION ERROR ==========");

                System.Diagnostics.Debug.WriteLine(
                    ex.ToString());

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
