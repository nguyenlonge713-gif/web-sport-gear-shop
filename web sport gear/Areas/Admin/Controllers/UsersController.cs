using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Helpers;
using System.Web.Mvc;
using web_sport_gear.Models;
using System.Text;
namespace web_sport_gear.Areas.Admin.Controllers
{
    public class UsersController : AdminBaseController
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"].ConnectionString;
            }
        }

        // DANH SACH TAI KHOAN
        public ActionResult Index(string keyword, string role)
        {
            List<AdminUserViewModel> users =
                new List<AdminUserViewModel>();

            if (role != "Admin" && role != "Customer")
            {
                role = "";
            }

            string sql = @"
                SELECT
                    UserID,
                    FullName,
                    Email,
                    Phone,
                    Role,
                    IsActive,
                    CreatedAt
                FROM dbo.Users
                WHERE 1 = 1";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += @"
                    AND (
                        Email LIKE @Keyword
                        OR Phone LIKE @Keyword
                    )";
            }

            if (!string.IsNullOrEmpty(role))
            {
                sql += " AND Role = @Role";
            }

            sql += " ORDER BY CreatedAt DESC";

            using (SqlConnection conn =
                new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd =
                    new SqlCommand(sql, conn))
                {
                    if (!string.IsNullOrWhiteSpace(keyword))
                    {
                        cmd.Parameters.Add(
                            "@Keyword",
                            SqlDbType.NVarChar,
                            255
                        ).Value = "%" + keyword.Trim() + "%";
                    }

                    if (!string.IsNullOrEmpty(role))
                    {
                        cmd.Parameters.Add(
                            "@Role",
                            SqlDbType.NVarChar,
                            50
                        ).Value = role;
                    }

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            AdminUserViewModel user =
                                new AdminUserViewModel();

                            user.UserID =
                                Convert.ToInt32(reader["UserID"]);

                            user.FullName =
                                reader["FullName"] == DBNull.Value
                                ? ""
                                : reader["FullName"].ToString();

                            user.Email =
                                reader["Email"] == DBNull.Value
                                ? ""
                                : reader["Email"].ToString();

                            user.Phone =
                                reader["Phone"] == DBNull.Value
                                ? ""
                                : reader["Phone"].ToString();

                            user.Role =
                                reader["Role"] == DBNull.Value
                                ? ""
                                : reader["Role"].ToString();

                            user.IsActive =
                                reader["IsActive"] != DBNull.Value
                                && Convert.ToBoolean(reader["IsActive"]);

                            user.CreatedAt =
                                reader["CreatedAt"] == DBNull.Value
                                ? DateTime.MinValue
                                : Convert.ToDateTime(reader["CreatedAt"]);

                            users.Add(user);
                        }
                    }
                }
            }

            ViewBag.Keyword = keyword;
            ViewBag.Role = role;

            return View(users);
        }
        // =========================================
        // THEM TAI KHOAN MOI
        // =========================================

        // GET: Admin/Users/Create
        public ActionResult Create()
        {
            SetRoleList();

            AdminUserCreateViewModel model =
                new AdminUserCreateViewModel();

            model.Role = "Customer";

            return View(model);
        }

        // POST: Admin/Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AdminUserCreateViewModel model)
        {
            if (model == null)
            {
                model = new AdminUserCreateViewModel();
            }

            // CHUAN HOA DU LIEU
            model.FullName = (model.FullName ?? "")
                .Trim()
                .Normalize(NormalizationForm.FormC);

            model.Email = (model.Email ?? "").Trim().ToLowerInvariant();
            model.Phone = (model.Phone ?? "").Trim();
            model.Role = (model.Role ?? "").Trim();

            // KIEM TRA LAI DATA ANNOTATION SAU KHI CHUAN HOA
            ModelState.Clear();
            TryValidateModel(model);

            SetRoleList(model.Role);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                bool emailExists;
                bool phoneExists;

                CheckDuplicateContact(
                    model.Email,
                    model.Phone,
                    0,
                    out emailExists,
                    out phoneExists
                );

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email này đã được sử dụng."
                    );
                }

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "Số điện thoại này đã được sử dụng."
                    );
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // MA HOA MAT KHAU
                string passwordHash =
                    Crypto.HashPassword(model.Password);

                // THEM TAI KHOAN
                string sql = @"
            INSERT INTO dbo.Users
            (
                FullName,
                Email,
                Phone,
                PasswordHash,
                Role
            )
            VALUES
            (
                @FullName,
                @Email,
                @Phone,
                @PasswordHash,
                @Role
            )";

                using (SqlConnection conn =
                    new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd =
                        new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add(
                            "@FullName",
                            SqlDbType.NVarChar,
                            100
                        ).Value = model.FullName;

                        cmd.Parameters.Add(
                            "@Email",
                            SqlDbType.NVarChar,
                            255
                        ).Value = model.Email;

                        cmd.Parameters.Add(
                            "@Phone",
                            SqlDbType.NVarChar,
                            20
                        ).Value = model.Phone;

                        cmd.Parameters.Add(
                            "@PasswordHash",
                            SqlDbType.NVarChar,
                            -1
                        ).Value = passwordHash;

                        cmd.Parameters.Add(
                            "@Role",
                            SqlDbType.NVarChar,
                            50
                        ).Value = model.Role;

                        conn.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["Success"] =
                    "Thêm tài khoản mới thành công.";

                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(
                        "",
                        "Email hoặc số điện thoại đã tồn tại."
                    );
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        "Đã xảy ra lỗi khi thêm tài khoản."
                    );
                }

                SetRoleList(model.Role);
                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Đã xảy ra lỗi khi thêm tài khoản."
                );

                SetRoleList(model.Role);
                return View(model);
            }
        }


        // =========================================
        // SUA THONG TIN TAI KHOAN
        // =========================================

        // GET: Admin/Users/Edit/5
        public ActionResult Edit(int id)
        {
            if (id <= 0)
            {
                TempData["Error"] =
                    "ID tài khoản không hợp lệ.";

                return RedirectToAction("Index");
            }

            string sql = @"
        SELECT UserID, FullName, Email, Phone
        FROM dbo.Users
        WHERE UserID = @UserID";

            using (SqlConnection conn =
                new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd =
                    new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(
                        "@UserID",
                        SqlDbType.Int
                    ).Value = id;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            AdminUserEditViewModel model =
                                new AdminUserEditViewModel();

                            model.UserID =
                                Convert.ToInt32(reader["UserID"]);

                            model.FullName =
                                reader["FullName"] == DBNull.Value
                                ? ""
                                : reader["FullName"].ToString();

                            model.Email =
                                reader["Email"] == DBNull.Value
                                ? ""
                                : reader["Email"].ToString();

                            model.Phone =
                                reader["Phone"] == DBNull.Value
                                ? ""
                                : reader["Phone"].ToString();

                            return View(model);
                        }
                    }
                }
            }

            TempData["Error"] =
                "Không tìm thấy tài khoản cần sửa.";

            return RedirectToAction("Index");
        }


        // POST: Admin/Users/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AdminUserEditViewModel model)
        {
            if (model == null)
            {
                TempData["Error"] =
                    "Dữ liệu tài khoản không hợp lệ.";

                return RedirectToAction("Index");
            }

            if (model.UserID <= 0)
            {
                TempData["Error"] =
                    "ID tài khoản không hợp lệ.";

                return RedirectToAction("Index");
            }

            // CHUAN HOA DU LIEU
            model.FullName = (model.FullName ?? "")
    .Trim()
    .Normalize(NormalizationForm.FormC);
            model.Email = (model.Email ?? "").Trim().ToLowerInvariant();
            model.Phone = (model.Phone ?? "").Trim();

            // KIEM TRA LAI DATA ANNOTATION SAU KHI CHUAN HOA
            ModelState.Clear();
            TryValidateModel(model);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // KIEM TRA EMAIL VA SDT CO BI TRUNG KHONG
                bool emailExists;
                bool phoneExists;

                CheckDuplicateContact(
                    model.Email,
                    model.Phone,
                    model.UserID,
                    out emailExists,
                    out phoneExists
                );

                if (emailExists)
                {
                    ModelState.AddModelError(
                        "Email",
                        "Email này đã được sử dụng bởi tài khoản khác."
                    );
                }

                if (phoneExists)
                {
                    ModelState.AddModelError(
                        "Phone",
                        "Số điện thoại này đã được sử dụng bởi tài khoản khác."
                    );
                }

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // CHI CAP NHAT HO TEN, EMAIL VA SDT
                // KHONG THAY DOI ROLE HOAC PASSWORDHASH
                string sql = @"
            UPDATE dbo.Users
            SET
                FullName = @FullName,
                Email = @Email,
                Phone = @Phone
            WHERE UserID = @UserID";

                using (SqlConnection conn =
                    new SqlConnection(ConnectionString))
                {
                    using (SqlCommand cmd =
                        new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add(
                            "@FullName",
                            SqlDbType.NVarChar,
                            100
                        ).Value = model.FullName;

                        cmd.Parameters.Add(
                            "@Email",
                            SqlDbType.NVarChar,
                            255
                        ).Value = model.Email;

                        cmd.Parameters.Add(
                            "@Phone",
                            SqlDbType.NVarChar,
                            20
                        ).Value = model.Phone;

                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = model.UserID;

                        conn.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 0)
                        {
                            TempData["Error"] =
                                "Không tìm thấy tài khoản cần sửa.";

                            return RedirectToAction("Index");
                        }
                    }
                }

                TempData["Success"] =
                    "Cập nhật thông tin tài khoản thành công.";

                return RedirectToAction("Index");
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(
                        "",
                        "Email hoặc số điện thoại đã tồn tại."
                    );
                }
                else
                {
                    ModelState.AddModelError(
                        "",
                        "Đã xảy ra lỗi khi cập nhật tài khoản."
                    );
                }

                return View(model);
            }
            catch (Exception)
            {
                ModelState.AddModelError(
                    "",
                    "Đã xảy ra lỗi khi cập nhật tài khoản."
                );

                return View(model);
            }
        }


        // =========================================
        // HAM HO TRO
        // =========================================

        // DANH SACH VAI TRO CHO FORM CREATE
        private void SetRoleList(string selectedRole = "Customer")
        {
            ViewBag.RoleList = new List<SelectListItem>
    {
        new SelectListItem
        {
            Text = "Customer",
            Value = "Customer",
            Selected = selectedRole == "Customer"
        },
        new SelectListItem
        {
            Text = "Admin",
            Value = "Admin",
            Selected = selectedRole == "Admin"
        }
    };
        }


        // KIEM TRA EMAIL VA SDT DA DUOC TAI KHOAN KHAC SU DUNG CHUA
        private void CheckDuplicateContact(
            string email,
            string phone,
            int excludeUserId,
            out bool emailExists,
            out bool phoneExists)
        {
            emailExists = false;
            phoneExists = false;

            string sql = @"
        SELECT Email, Phone
        FROM dbo.Users
        WHERE (Email = @Email OR Phone = @Phone)
        AND UserID <> @UserID";

            using (SqlConnection conn =
                new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd =
                    new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add(
                        "@Email",
                        SqlDbType.NVarChar,
                        255
                    ).Value = email;

                    cmd.Parameters.Add(
                        "@Phone",
                        SqlDbType.NVarChar,
                        20
                    ).Value = phone;

                    cmd.Parameters.Add(
                        "@UserID",
                        SqlDbType.Int
                    ).Value = excludeUserId;

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string existingEmail =
                                reader["Email"] == DBNull.Value
                                ? ""
                                : reader["Email"].ToString();

                            string existingPhone =
                                reader["Phone"] == DBNull.Value
                                ? ""
                                : reader["Phone"].ToString();

                            if (string.Equals(
                                existingEmail,
                                email,
                                StringComparison.OrdinalIgnoreCase))
                            {
                                emailExists = true;
                            }

                            if (string.Equals(
                                existingPhone,
                                phone,
                                StringComparison.Ordinal))
                            {
                                phoneExists = true;
                            }
                        }
                    }
                }
            }
        }
        // THAY DOI QUYEN TAI KHOAN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeRole(
            int id,
            string role,
            string keyword,
            string filterRole)
        {
            int currentUserId;

            if (!int.TryParse(
                Convert.ToString(Session["UserID"]),
                out currentUserId))
            {
                TempData["Error"] =
                    "Không xác định được tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            if (id == currentUserId)
            {
                TempData["Error"] =
                    "Bạn không thể tự thay đổi quyền tài khoản của mình.";

                return RedirectToIndex(keyword, filterRole);
            }

            if (role != "Admin" && role != "Customer")
            {
                TempData["Error"] =
                    "Vai trò được chọn không hợp lệ.";

                return RedirectToIndex(keyword, filterRole);
            }

            SqlTransaction transaction = null;

            using (SqlConnection conn =
                new SqlConnection(ConnectionString))
            {
                try
                {
                    conn.Open();
                    transaction = conn.BeginTransaction();

                    string oldRole = "";
                    bool isActive = false;
                    bool userExists = false;

                    string selectSql = @"
                        SELECT Role, IsActive
                        FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                        WHERE UserID = @UserID";

                    using (SqlCommand cmd =
                        new SqlCommand(selectSql, conn, transaction))
                    {
                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = id;

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userExists = true;

                                oldRole = reader["Role"].ToString();

                                isActive =
                                    reader["IsActive"] != DBNull.Value
                                    && Convert.ToBoolean(reader["IsActive"]);
                            }
                        }
                    }

                    if (!userExists)
                    {
                        throw new InvalidOperationException(
                            "Không tìm thấy tài khoản cần thay đổi."
                        );
                    }

                    // KHONG CHO HA QUYEN ADMIN CUOI CUNG
                    if (
                        oldRole == "Admin"
                        && role == "Customer"
                        && isActive
                    )
                    {
                        string countSql = @"
                            SELECT COUNT(*)
                            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                            WHERE Role = 'Admin'
                            AND IsActive = 1";

                        int activeAdminCount;

                        using (SqlCommand cmd =
                            new SqlCommand(
                                countSql,
                                conn,
                                transaction))
                        {
                            activeAdminCount =
                                Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        if (activeAdminCount <= 1)
                        {
                            throw new InvalidOperationException(
                                "Không thể hạ quyền Admin cuối cùng đang hoạt động."
                            );
                        }
                    }

                    string updateSql = @"
                        UPDATE dbo.Users
                        SET Role = @Role
                        WHERE UserID = @UserID";

                    using (SqlCommand cmd =
                        new SqlCommand(updateSql, conn, transaction))
                    {
                        cmd.Parameters.Add(
                            "@Role",
                            SqlDbType.NVarChar,
                            50
                        ).Value = role;

                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = id;

                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();

                    TempData["Success"] =
                        "Cập nhật quyền tài khoản thành công.";
                }
                catch (InvalidOperationException ex)
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }
                    }

                    TempData["Error"] = ex.Message;
                }
                catch (Exception)
                {
                    if (transaction != null)
                    {
                        try
                        {
                            transaction.Rollback();
                        }
                        catch
                        {
                        }
                    }

                    TempData["Error"] =
                        "Đã xảy ra lỗi khi cập nhật quyền tài khoản.";
                }
            }

            return RedirectToIndex(keyword, filterRole);
        }


        // DAT LAI MAT KHAU
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(
            int id,
            string newPassword,
            string confirmPassword,
            string keyword,
            string filterRole)
        {
            int currentUserId;

            // KIEM TRA ADMIN DANG DANG NHAP
            if (!int.TryParse(
                Convert.ToString(Session["UserID"]),
                out currentUserId))
            {
                TempData["Error"] =
                    "Không xác định được tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            // KHONG CHO TU DAT LAI MAT KHAU
            if (id == currentUserId)
            {
                TempData["Error"] =
                    "Bạn không thể tự đặt lại mật khẩu tại chức năng này.";

                return RedirectToIndex(keyword, filterRole);
            }

            // KIEM TRA MAT KHAU MOI
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["Error"] =
                    "Vui lòng nhập mật khẩu mới.";

                return RedirectToIndex(keyword, filterRole);
            }

            // KIEM TRA MAT KHAU XAC NHAN
            if (string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] =
                    "Vui lòng nhập mật khẩu xác nhận.";

                return RedirectToIndex(keyword, filterRole);
            }

            // KIEM TRA DO DAI MAT KHAU
            if (newPassword.Length < 8 || newPassword.Length > 128)
            {
                TempData["Error"] =
                    "Mật khẩu phải có từ 8 đến 128 ký tự.";

                return RedirectToIndex(keyword, filterRole);
            }

            // KIEM TRA DO PHUC TAP MAT KHAU
            string passwordPattern =
                @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9\s]).{8,128}$";

            if (!Regex.IsMatch(newPassword, passwordPattern))
            {
                TempData["Error"] =
                    "Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt.";

                return RedirectToIndex(keyword, filterRole);
            }

            // SO SANH CHINH XAC HAI MAT KHAU
            if (!string.Equals(
                newPassword,
                confirmPassword,
                StringComparison.Ordinal))
            {
                TempData["Error"] =
                    "Mật khẩu mới và mật khẩu xác nhận không trùng khớp.";

                return RedirectToIndex(keyword, filterRole);
            }

            try
            {
                // KIEM TRA TAI KHOAN CO TON TAI KHONG
                string checkSql = @"
            SELECT COUNT(*)
            FROM dbo.Users
            WHERE UserID = @UserID";

                using (SqlConnection conn =
                    new SqlConnection(ConnectionString))
                {
                    conn.Open();

                    using (SqlCommand checkCmd =
                        new SqlCommand(checkSql, conn))
                    {
                        checkCmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = id;

                        int userCount =
                            Convert.ToInt32(checkCmd.ExecuteScalar());

                        if (userCount == 0)
                        {
                            TempData["Error"] =
                                "Không tìm thấy tài khoản cần đặt lại mật khẩu.";

                            return RedirectToIndex(keyword, filterRole);
                        }
                    }

                    // HASH MAT KHAU
                    string passwordHash =
                        Crypto.HashPassword(newPassword);

                    // CAP NHAT MAT KHAU
                    string updateSql = @"
                UPDATE dbo.Users
                SET PasswordHash = @PasswordHash
                WHERE UserID = @UserID";

                    using (SqlCommand cmd =
                        new SqlCommand(updateSql, conn))
                    {
                        cmd.Parameters.Add(
                            "@PasswordHash",
                            SqlDbType.NVarChar,
                            -1
                        ).Value = passwordHash;

                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = id;

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected == 1)
                        {
                            TempData["Success"] =
                                "Đặt lại mật khẩu thành công.";
                        }
                        else
                        {
                            TempData["Error"] =
                                "Không thể cập nhật mật khẩu tài khoản.";
                        }
                    }
                }
            }
            catch (Exception)
            {
                TempData["Error"] =
                    "Đã xảy ra lỗi khi đặt lại mật khẩu.";
            }

            return RedirectToIndex(keyword, filterRole);
        }


        // GIU LAI BO LOC SAU KHI THAO TAC
        private ActionResult RedirectToIndex(
            string keyword,
            string role)
        {
            if (role != "Admin" && role != "Customer")
            {
                role = "";
            }

            return RedirectToAction(
                "Index",
                new
                {
                    keyword = keyword,
                    role = role
                }
            );
        }
    }
}
