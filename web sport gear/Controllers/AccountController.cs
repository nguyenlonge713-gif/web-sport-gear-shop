
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Controllers
{
    public class AccountController : Controller
    {
        // Lay chuoi ket noi tu Web.config
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"]
                    .ConnectionString;
            }
        }

        // Kiem tra so dien thoai va tra ve loi cu the
        private string GetPhoneValidationError(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return "Vui lòng nhập số điện thoại.";
            }

            // Chi cho phep chu so tu 0 den 9
            if (!Regex.IsMatch(phone, @"^[0-9]+$"))
            {
                return "Số điện thoại chỉ được chứa chữ số từ 0 đến 9.";
            }

            // Phai co dung 10 chu so
            if (phone.Length != 10)
            {
                return "Số điện thoại phải có đúng 10 chữ số.";
            }

            // Phai bat dau bang so 0
            if (phone[0] != '0')
            {
                return "Số điện thoại phải bắt đầu bằng số 0.";
            }

            // Khong cho phep tat ca chu so giong nhau
            if (phone.Distinct().Count() == 1)
            {
                return "Số điện thoại không được chứa toàn bộ chữ số giống nhau.";
            }

            return null;
        }

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        // GET: Account/Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(string email, string password)
        {
            email = (email ?? "").Trim();

            if (string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError(
                    "",
                    "Vui long nhap email/so dien thoai va mat khau.");

                return View();
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT UserID, FullName, PasswordHash, Role, IsActive
                        FROM dbo.Users
                        WHERE Email = @Login
                           OR Phone = @Login;";

                    int userId = 0;
                    string fullName = "";
                    string passwordHash = "";
                    string role = "";
                    bool isActive = false;

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@Login", email);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userId = Convert.ToInt32(reader["UserID"]);
                                fullName = reader["FullName"].ToString();
                                passwordHash = reader["PasswordHash"].ToString();
                                role = reader["Role"].ToString();
                                isActive = Convert.ToBoolean(reader["IsActive"]);
                            }
                        }
                    }

                    if (userId == 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Email/so dien thoai hoac mat khau khong dung.");

                        return View();
                    }

                    if (!isActive)
                    {
                        ModelState.AddModelError(
                            "",
                            "Tai khoan cua ban da bi vo hieu hoa.");

                        return View();
                    }

                    bool passwordCorrect;

                    try
                    {
                        passwordCorrect =
                            Crypto.VerifyHashedPassword(
                                passwordHash,
                                password);
                    }
                    catch (FormatException)
                    {
                        passwordCorrect = false;
                    }

                    if (!passwordCorrect)
                    {
                        ModelState.AddModelError(
                            "",
                            "Email/so dien thoai hoac mat khau khong dung.");

                        return View();
                    }

                    // Luu thong tin dang nhap vao Session
                    Session["UserID"] = userId;
                    Session["FullName"] = fullName;
                    Session["Role"] = role;
                }

                return RedirectToAction("Index", "Home");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the truy cap co so du lieu. Vui long thu lai sau.");

                return View();
            }
            catch (ConfigurationErrorsException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong tim thay cau hinh ket noi GearSportDB.");

                return View();
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the ket noi co so du lieu. Vui long kiem tra cau hinh.");

                return View();
            }
        }

        public ActionResult OrderHistory()
        {
            // Kiem tra nguoi dung da dang nhap chua
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId;

            if (!int.TryParse(Session["UserID"].ToString(), out userId))
            {
                Session.Clear();
                Session.Abandon();
                return RedirectToAction("Login", "Account");
            }

            List<CustomerOrderHistoryViewModel> orders =
                new List<CustomerOrderHistoryViewModel>();

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT
                        o.OrderID,
                        o.ReceiverName,
                        o.ReceiverPhone,
                        o.ShippingAddress,
                        o.TotalAmount,
                        o.PaymentMethod,
                        o.PaymentStatus,
                        o.OrderStatus,
                        o.CreatedAt,
                        d.OrderDetailID,
                        d.ProductID,
                        d.ProductName,
                        d.Quantity,
                        d.UnitPrice
                    FROM dbo.Orders o
                    LEFT JOIN dbo.OrderDetails d
                        ON o.OrderID = d.OrderID
                    WHERE o.UserID = @UserID
                    ORDER BY
                        o.CreatedAt DESC,
                        o.OrderID DESC,
                        d.OrderDetailID ASC;";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@UserID", userId);

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        Dictionary<int, CustomerOrderHistoryViewModel> orderDictionary =
                            new Dictionary<int, CustomerOrderHistoryViewModel>();

                        while (reader.Read())
                        {
                            int orderId = reader.GetInt32(
                                reader.GetOrdinal("OrderID"));

                            CustomerOrderHistoryViewModel order;

                            if (!orderDictionary.TryGetValue(orderId, out order))
                            {
                                order = new CustomerOrderHistoryViewModel();

                                order.OrderID = orderId;
                                order.ReceiverName = reader.GetString(
                                    reader.GetOrdinal("ReceiverName"));

                                order.ReceiverPhone = reader.GetString(
                                    reader.GetOrdinal("ReceiverPhone"));

                                order.ShippingAddress = reader.GetString(
                                    reader.GetOrdinal("ShippingAddress"));

                                order.TotalAmount = reader.GetDecimal(
                                    reader.GetOrdinal("TotalAmount"));

                                order.PaymentMethod = reader.GetString(
                                    reader.GetOrdinal("PaymentMethod"));

                                order.PaymentStatus = reader.GetString(
                                    reader.GetOrdinal("PaymentStatus"));

                                order.OrderStatus = reader.GetString(
                                    reader.GetOrdinal("OrderStatus"));

                                order.CreatedAt = reader.GetDateTime(
                                    reader.GetOrdinal("CreatedAt"));

                                orderDictionary.Add(orderId, order);
                                orders.Add(order);
                            }

                            int detailOrdinal = reader.GetOrdinal("OrderDetailID");

                            if (!reader.IsDBNull(detailOrdinal))
                            {
                                CustomerOrderItemViewModel item =
                                    new CustomerOrderItemViewModel();

                                item.OrderDetailID = reader.GetInt32(detailOrdinal);

                                item.ProductID = reader.GetInt32(
                                    reader.GetOrdinal("ProductID"));

                                item.ProductName = reader.GetString(
                                    reader.GetOrdinal("ProductName"));

                                item.Quantity = reader.GetInt32(
                                    reader.GetOrdinal("Quantity"));

                                item.UnitPrice = reader.GetDecimal(
                                    reader.GetOrdinal("UnitPrice"));

                                order.Items.Add(item);
                            }
                        }
                    }
                }
            }

            return View(orders);
        }

        // GET: Account/Profile
        [HttpGet]
        public new ActionResult Profile()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                        SELECT UserID, FullName, Email, Phone,
                               DateOfBirth, Gender, Address, Avatar
                        FROM dbo.Users
                        WHERE UserID = @UserID AND IsActive = 1;";

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                Session.Clear();
                                Session.Abandon();

                                return RedirectToAction("Login");
                            }

                            UserProfileViewModel model =
                                new UserProfileViewModel
                                {
                                    UserID = Convert.ToInt32(reader["UserID"]),
                                    FullName = reader["FullName"].ToString(),
                                    Email = reader["Email"].ToString(),

                                    Phone = reader["Phone"] == DBNull.Value
                                        ? ""
                                        : reader["Phone"].ToString(),

                                    DateOfBirth =
                                        reader["DateOfBirth"] == DBNull.Value
                                        ? (DateTime?)null
                                        : Convert.ToDateTime(reader["DateOfBirth"]),

                                    Gender = reader["Gender"] == DBNull.Value
                                        ? ""
                                        : reader["Gender"].ToString(),

                                    Address = reader["Address"] == DBNull.Value
                                        ? ""
                                        : reader["Address"].ToString(),

                                    Avatar = reader["Avatar"] == DBNull.Value
                                        ? ""
                                        : reader["Avatar"].ToString()
                                };

                            return View(model);
                        }
                    }
                }
            }
            catch (SqlException)
            {
                TempData["ProfileError"] =
                    "Khong the tai thong tin ho so. Vui long thu lai.";

                return RedirectToAction("Index", "Home");
            }
            catch (ConfigurationErrorsException)
            {
                TempData["ProfileError"] =
                    "Khong tim thay cau hinh ket noi GearSportDB.";

                return RedirectToAction("Index", "Home");
            }
            catch (InvalidOperationException)
            {
                TempData["ProfileError"] =
                    "Khong the ket noi co so du lieu.";

                return RedirectToAction("Index", "Home");
            }
        }

        // POST: Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateProfile(UserProfileViewModel model)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            // Chuan hoa du lieu
            model.FullName = (model.FullName ?? "").Trim();
            model.Phone = (model.Phone ?? "").Trim();
            model.Gender = (model.Gender ?? "").Trim();
            model.Address = (model.Address ?? "").Trim();

            // Kiem tra ho va ten
            if (string.IsNullOrWhiteSpace(model.FullName))
            {
                ModelState.AddModelError(
                    "FullName",
                    "Vui long nhap ho va ten.");
            }
            else if (model.FullName.Length > 100)
            {
                ModelState.AddModelError(
                    "FullName",
                    "Ho va ten khong duoc vuot qua 100 ky tu.");
            }

            // Kiem tra so dien thoai
            // Cho phep de trong tai trang ho so
            if (!string.IsNullOrWhiteSpace(model.Phone))
            {
                string phoneError =
                    GetPhoneValidationError(model.Phone);

                if (phoneError != null)
                {
                    ModelState.AddModelError("Phone", phoneError);
                }
            }

            // Kiem tra ngay sinh
            if (model.DateOfBirth.HasValue &&
                model.DateOfBirth.Value.Date > DateTime.Today)
            {
                ModelState.AddModelError(
                    "DateOfBirth",
                    "Ngay sinh khong duoc lon hon ngay hien tai.");
            }

            // Kiem tra gioi tinh
            if (!string.IsNullOrWhiteSpace(model.Gender) &&
                model.Gender != "Nam" &&
                model.Gender != "Nu" &&
                model.Gender != "Khac")
            {
                ModelState.AddModelError(
                    "Gender",
                    "Gioi tinh khong hop le.");
            }

            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string sql = @"
                        UPDATE dbo.Users
                        SET FullName = @FullName,
                            Phone = @Phone,
                            DateOfBirth = @DateOfBirth,
                            Gender = @Gender,
                            Address = @Address
                        WHERE UserID = @UserID
                          AND IsActive = 1;";

                    using (SqlCommand command =
                        new SqlCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@FullName", model.FullName);

                        command.Parameters.AddWithValue(
                            "@Phone",
                            string.IsNullOrWhiteSpace(model.Phone)
                                ? (object)DBNull.Value
                                : model.Phone);

                        command.Parameters.AddWithValue(
                            "@DateOfBirth",
                            model.DateOfBirth.HasValue
                                ? (object)model.DateOfBirth.Value.Date
                                : DBNull.Value);

                        command.Parameters.AddWithValue(
                            "@Gender",
                            string.IsNullOrWhiteSpace(model.Gender)
                                ? (object)DBNull.Value
                                : model.Gender);

                        command.Parameters.AddWithValue(
                            "@Address",
                            string.IsNullOrWhiteSpace(model.Address)
                                ? (object)DBNull.Value
                                : model.Address);

                        command.Parameters.AddWithValue("@UserID", userId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            Session.Clear();
                            Session.Abandon();

                            return RedirectToAction("Login");
                        }
                    }
                }

                Session["FullName"] = model.FullName;

                TempData["ProfileSuccess"] =
                    "Cap nhat thong tin ca nhan thanh cong!";

                return RedirectToAction("Profile");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the cap nhat ho so. Vui long thu lai sau.");

                return View("Profile", model);
            }
            catch (ConfigurationErrorsException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong tim thay cau hinh ket noi GearSportDB.");

                return View("Profile", model);
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the ket noi co so du lieu.");

                return View("Profile", model);
            }
        }

        // POST: Account/UploadAvatar
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAvatar(HttpPostedFileBase avatarFile)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            if (avatarFile == null || avatarFile.ContentLength == 0)
            {
                TempData["ProfileError"] = "Vui long chon anh dai dien.";
                return RedirectToAction("Profile");
            }

            if (avatarFile.ContentLength > 2 * 1024 * 1024)
            {
                TempData["ProfileError"] = "Anh khong duoc vuot qua 2MB.";
                return RedirectToAction("Profile");
            }

            string extension =
                Path.GetExtension(avatarFile.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                TempData["ProfileError"] =
                    "Chi chap nhan anh JPG, JPEG hoac PNG.";

                return RedirectToAction("Profile");
            }

            byte[] header = new byte[8];

            int bytesRead =
                avatarFile.InputStream.Read(header, 0, header.Length);

            bool isJpeg = bytesRead >= 3 &&
                          header[0] == 0xFF &&
                          header[1] == 0xD8 &&
                          header[2] == 0xFF;

            bool isPng = bytesRead >= 8 &&
                         header[0] == 0x89 &&
                         header[1] == 0x50 &&
                         header[2] == 0x4E &&
                         header[3] == 0x47 &&
                         header[4] == 0x0D &&
                         header[5] == 0x0A &&
                         header[6] == 0x1A &&
                         header[7] == 0x0A;

            avatarFile.InputStream.Position = 0;

            if (!((extension == ".jpg" || extension == ".jpeg") && isJpeg) &&
                !(extension == ".png" && isPng))
            {
                TempData["ProfileError"] =
                    "Noi dung file khong phai anh JPG hoac PNG hop le.";

                return RedirectToAction("Profile");
            }

            string folderPath =
                Server.MapPath("~/Content/Uploads/Avatars/");

            string newFileName =
                Guid.NewGuid().ToString("N") + extension;

            string newFilePath =
                Path.Combine(folderPath, newFileName);

            string avatarUrl =
                "~/Content/Uploads/Avatars/" + newFileName;

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                avatarFile.SaveAs(newFilePath);

                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string oldAvatar = null;

                    string selectSql = @"
                        SELECT Avatar
                        FROM dbo.Users
                        WHERE UserID = @UserID AND IsActive = 1;";

                    using (SqlCommand command =
                        new SqlCommand(selectSql, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        object result = command.ExecuteScalar();

                        if (result == null)
                        {
                            Session.Clear();
                            Session.Abandon();

                            System.IO.File.Delete(newFilePath);

                            return RedirectToAction("Login");
                        }

                        oldAvatar = result == DBNull.Value
                            ? null
                            : result.ToString();
                    }

                    string updateSql = @"
                        UPDATE dbo.Users
                        SET Avatar = @Avatar
                        WHERE UserID = @UserID AND IsActive = 1;";

                    using (SqlCommand command =
                        new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue("@Avatar", avatarUrl);
                        command.Parameters.AddWithValue("@UserID", userId);

                        command.ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(oldAvatar) &&
                        oldAvatar.StartsWith(
                            "~/Content/Uploads/Avatars/",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        string oldFileName = Path.GetFileName(oldAvatar);

                        string oldFilePath =
                            Path.Combine(folderPath, oldFileName);

                        if (System.IO.File.Exists(oldFilePath) &&
                            !string.Equals(
                                oldFilePath,
                                newFilePath,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                TempData["ProfileSuccess"] =
                    "Cap nhat anh dai dien thanh cong!";

                return RedirectToAction("Profile");
            }
            catch (Exception)
            {
                if (System.IO.File.Exists(newFilePath))
                {
                    System.IO.File.Delete(newFilePath);
                }

                TempData["ProfileError"] =
                    "Khong the tai anh dai dien. Vui long thu lai.";

                return RedirectToAction("Profile");
            }
        }

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
        }

        // GET: Account/ChangePassword
        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            return View(new ChangePasswordViewModel());
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserID"]);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    string selectSql = @"
                        SELECT PasswordHash
                        FROM dbo.Users
                        WHERE UserID = @UserID
                          AND IsActive = 1;";

                    string passwordHash = null;

                    using (SqlCommand command =
                        new SqlCommand(selectSql, connection))
                    {
                        command.Parameters.AddWithValue("@UserID", userId);

                        object result = command.ExecuteScalar();

                        if (result == null || result == DBNull.Value)
                        {
                            Session.Clear();
                            Session.Abandon();

                            return RedirectToAction("Login");
                        }

                        passwordHash = result.ToString();
                    }

                    bool currentPasswordCorrect = false;

                    try
                    {
                        currentPasswordCorrect =
                            Crypto.VerifyHashedPassword(
                                passwordHash,
                                model.CurrentPassword);
                    }
                    catch (FormatException)
                    {
                        currentPasswordCorrect = false;
                    }

                    if (!currentPasswordCorrect)
                    {
                        ModelState.AddModelError(
                            "CurrentPassword",
                            "Mat khau hien tai khong chinh xac.");

                        return View(model);
                    }

                    if (Crypto.VerifyHashedPassword(
                        passwordHash,
                        model.NewPassword))
                    {
                        ModelState.AddModelError(
                            "NewPassword",
                            "Mat khau moi phai khac mat khau hien tai.");

                        return View(model);
                    }

                    string newPasswordHash =
                        Crypto.HashPassword(model.NewPassword);

                    string updateSql = @"
                        UPDATE dbo.Users
                        SET PasswordHash = @PasswordHash
                        WHERE UserID = @UserID
                          AND IsActive = 1;";

                    using (SqlCommand command =
                        new SqlCommand(updateSql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@PasswordHash",
                            newPasswordHash);

                        command.Parameters.AddWithValue(
                            "@UserID",
                            userId);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            Session.Clear();
                            Session.Abandon();

                            return RedirectToAction("Login");
                        }
                    }
                }

                TempData["PasswordSuccess"] =
                    "Doi mat khau thanh cong!";

                return RedirectToAction("ChangePassword");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the cap nhat mat khau. Vui long thu lai.");

                return View(model);
            }
            catch (ConfigurationErrorsException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong tim thay cau hinh ket noi GearSportDB.");

                return View(model);
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the ket noi co so du lieu.");

                return View(model);
            }
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(
            string FullName,
            string Email,
            string Phone,
            string Password,
            string ConfirmPassword)
        {
            FullName = (FullName ?? "").Trim();
            Email = (Email ?? "").Trim();
            Phone = (Phone ?? "").Trim();

            // Kiem tra ho va ten
            if (string.IsNullOrWhiteSpace(FullName))
            {
                ModelState.AddModelError(
                    "FullName",
                    "Vui long nhap ho va ten.");
            }
            else if (FullName.Length > 100)
            {
                ModelState.AddModelError(
                    "FullName",
                    "Ho va ten khong duoc vuot qua 100 ky tu.");
            }

            // Kiem tra email
            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(
                    "Email",
                    "Vui long nhap email.");
            }
            else if (Email.Length > 255 ||
                     !Regex.IsMatch(
                         Email,
                         @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError(
                    "Email",
                    "Email khong hop le.");
            }

            // Kiem tra so dien thoai voi thong bao rieng
            string phoneError = GetPhoneValidationError(Phone);

            if (phoneError != null)
            {
                ModelState.AddModelError("Phone", phoneError);
            }

            // Kiem tra mat khau
            if (string.IsNullOrEmpty(Password) || Password.Length < 8)
            {
                ModelState.AddModelError(
                    "Password",
                    "Mat khau phai co it nhat 8 ky tu.");
            }

            // Kiem tra xac nhan mat khau
            if (Password != ConfirmPassword)
            {
                ModelState.AddModelError(
                    "ConfirmPassword",
                    "Mat khau xac nhan khong khop.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                {
                    connection.Open();

                    // Kiem tra email da ton tai
                    string checkEmailSql = @"
                        SELECT COUNT(1)
                        FROM dbo.Users
                        WHERE Email = @Email;";

                    using (SqlCommand command =
                        new SqlCommand(checkEmailSql, connection))
                    {
                        command.Parameters.AddWithValue("@Email", Email);

                        int emailExists =
                            (int)command.ExecuteScalar();

                        if (emailExists > 0)
                        {
                            ModelState.AddModelError(
                                "Email",
                                "Email nay da duoc dang ky.");

                            return View();
                        }
                    }

                    // Kiem tra so dien thoai da ton tai
                    string checkPhoneSql = @"
                        SELECT COUNT(1)
                        FROM dbo.Users
                        WHERE Phone = @Phone;";

                    using (SqlCommand command =
                        new SqlCommand(checkPhoneSql, connection))
                    {
                        command.Parameters.AddWithValue("@Phone", Phone);

                        int phoneExists =
                            (int)command.ExecuteScalar();

                        if (phoneExists > 0)
                        {
                            ModelState.AddModelError(
                                "Phone",
                                "Số điện thoại này đã được đăng ký.");

                            return View();
                        }
                    }

                    string passwordHash =
                        Crypto.HashPassword(Password);

                    string insertSql = @"
                        INSERT INTO dbo.Users
                            (FullName, Email, Phone, PasswordHash)
                        VALUES
                            (@FullName, @Email, @Phone, @PasswordHash);";

                    using (SqlCommand command =
                        new SqlCommand(insertSql, connection))
                    {
                        command.Parameters.AddWithValue(
                            "@FullName", FullName);

                        command.Parameters.AddWithValue(
                            "@Email", Email);

                        command.Parameters.AddWithValue(
                            "@Phone", Phone);

                        command.Parameters.AddWithValue(
                            "@PasswordHash", passwordHash);

                        command.ExecuteNonQuery();
                    }
                }

                TempData["RegisterSuccess"] =
                    "Dang ky thanh cong! Ban co the dang nhap.";

                return RedirectToAction("Login");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(
                        "",
                        "Email hoac so dien thoai da duoc dang ky.");

                    return View();
                }

                ModelState.AddModelError(
                    "",
                    "Khong the luu tai khoan. Vui long thu lai sau.");

                return View();
            }
            catch (ConfigurationErrorsException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong tim thay cau hinh ket noi GearSportDB.");

                return View();
            }
            catch (InvalidOperationException)
            {
                ModelState.AddModelError(
                    "",
                    "Khong the ket noi co so du lieu. Vui long kiem tra cau hinh.");

                return View();
            }
        }

        // POST: Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Index", "Home");
        }
    }
}
