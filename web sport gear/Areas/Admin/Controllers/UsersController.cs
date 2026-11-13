using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Helpers;
using System.Web.Mvc;
using web_sport_gear.Models;

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
