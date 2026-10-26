
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using System.Web.Mvc;

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

                    // Tim tai khoan bang email hoac so dien thoai
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

                    // Khong tim thay tai khoan
                    if (userId == 0)
                    {
                        ModelState.AddModelError(
                            "",
                            "Email/so dien thoai hoac mat khau khong dung.");

                        return View();
                    }

                    // Kiem tra tai khoan co dang hoat dong khong
                    if (!isActive)
                    {
                        ModelState.AddModelError(
                            "",
                            "Tai khoan cua ban da bi vo hieu hoa.");

                        return View();
                    }

                    // So sanh mat khau nhap vao voi mat khau da bam
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

                    // Dang nhap thanh cong: luu thong tin vao Session
                    Session["UserID"] = userId;
                    Session["FullName"] = fullName;
                    Session["Role"] = role;
                }

                // Chuyen den trang chu sau khi dang nhap
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

        // GET: Account/Register
        [HttpGet]
        public ActionResult Register()
        {
            return View();
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

            if (string.IsNullOrWhiteSpace(FullName))
            {
                ModelState.AddModelError(
                    "FullName", "Vui long nhap ho va ten.");
            }
            else if (FullName.Length > 100)
            {
                ModelState.AddModelError(
                    "FullName",
                    "Ho va ten khong duoc vuot qua 100 ky tu.");
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                ModelState.AddModelError(
                    "Email", "Vui long nhap email.");
            }
            else if (Email.Length > 255 ||
                     !Regex.IsMatch(Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ModelState.AddModelError(
                    "Email", "Email khong hop le.");
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                ModelState.AddModelError(
                    "Phone", "Vui long nhap so dien thoai.");
            }
            else if (Phone.Length > 20)
            {
                ModelState.AddModelError(
                    "Phone",
                    "So dien thoai khong duoc vuot qua 20 ky tu.");
            }

            if (string.IsNullOrEmpty(Password) || Password.Length < 8)
            {
                ModelState.AddModelError(
                    "Password",
                    "Mat khau phai co it nhat 8 ky tu.");
            }

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
                                "So dien thoai nay da duoc dang ky.");

                            return View();
                        }
                    }

                    string passwordHash = Crypto.HashPassword(Password);

                    string insertSql = @"
                        INSERT INTO dbo.Users
                            (FullName, Email, Phone, PasswordHash)
                        VALUES
                            (@FullName, @Email, @Phone, @PasswordHash);";

                    using (SqlCommand command =
                        new SqlCommand(insertSql, connection))
                    {
                        command.Parameters.AddWithValue("@FullName", FullName);
                        command.Parameters.AddWithValue("@Email", Email);
                        command.Parameters.AddWithValue("@Phone", Phone);
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
   