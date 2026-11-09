
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class UsersController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["GearSportDB"].ConnectionString;

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

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

        // DANH SÁCH TÀI KHOẢN
        public ActionResult Index(string keyword)
        {
            var check = CheckAdmin();
            if (check != null) return check;

            List<AdminUserViewModel> users =
                new List<AdminUserViewModel>();

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            {
                string sql = @"
                    SELECT UserID, FullName, Email, Phone,
                           Role, IsActive, CreatedAt
                    FROM dbo.Users
                    WHERE
                        (@Keyword IS NULL
                         OR FullName LIKE N'%' + @Keyword + N'%'
                         OR Email LIKE N'%' + @Keyword + N'%'
                         OR Phone LIKE N'%' + @Keyword + N'%')
                    ORDER BY CreatedAt DESC";

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@Keyword",
                        SqlDbType.NVarChar, 255).Value =
                        string.IsNullOrWhiteSpace(keyword)
                            ? (object)DBNull.Value
                            : keyword.Trim();

                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            users.Add(new AdminUserViewModel
                            {
                                UserID = Convert.ToInt32(reader["UserID"]),
                                FullName = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value
                                    ? ""
                                    : reader["Phone"].ToString(),
                                Role = reader["Role"].ToString(),
                                IsActive = Convert.ToBoolean(reader["IsActive"]),
                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }
            }

            ViewBag.Keyword = keyword;

            return View(users);
        }

        // KHÓA / MỞ KHÓA TÀI KHOẢN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            var check = CheckAdmin();
            if (check != null) return check;

            int currentUserID = Convert.ToInt32(Session["UserID"]);

            // Không cho phép tự khóa tài khoản đang đăng nhập
            if (id == currentUserID)
            {
                TempData["ErrorMessage"] =
                    "Bạn không thể tự khóa tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string selectSql = @"
                            SELECT Role, IsActive
                            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                            WHERE UserID = @UserID";

                        string role;
                        bool isActive;

                        using (SqlCommand cmd =
                            new SqlCommand(selectSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = id;

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Không tìm thấy tài khoản.");
                                }

                                role = reader["Role"].ToString();
                                isActive = Convert.ToBoolean(reader["IsActive"]);
                            }
                        }

                        // Không cho khóa Admin cuối cùng còn hoạt động
                        if (isActive && role == "Admin")
                        {
                            string countSql = @"
                                SELECT COUNT(*)
                                FROM dbo.Users
                                WHERE Role = N'Admin'
                                  AND IsActive = 1";

                            int activeAdmins;

                            using (SqlCommand cmd =
                                new SqlCommand(countSql, conn, transaction))
                            {
                                activeAdmins = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            if (activeAdmins <= 1)
                            {
                                throw new Exception(
                                    "Không thể khóa Admin cuối cùng đang hoạt động.");
                            }
                        }

                        string updateSql = @"
                            UPDATE dbo.Users
                            SET IsActive = @IsActive
                            WHERE UserID = @UserID";

                        using (SqlCommand cmd =
                            new SqlCommand(updateSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value =
                                !isActive;

                            cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = id;

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] = !isActive
                            ? "Đã mở khóa tài khoản thành công."
                            : "Đã khóa tài khoản thành công.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = ex.Message;
                    }
                }
            }

            return RedirectToAction("Index");
        }

        // THAY ĐỔI VAI TRÒ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangeRole(int id, string role)
        {
            var check = CheckAdmin();
            if (check != null) return check;

            int currentUserID = Convert.ToInt32(Session["UserID"]);

            if (id == currentUserID)
            {
                TempData["ErrorMessage"] =
                    "Bạn không thể tự thay đổi quyền của tài khoản đang đăng nhập.";

                return RedirectToAction("Index");
            }

            if (role != "Admin" && role != "Customer")
            {
                TempData["ErrorMessage"] = "Vai trò không hợp lệ.";
                return RedirectToAction("Index");
            }

            using (SqlConnection conn =
                new SqlConnection(connectionString))
            {
                conn.Open();

                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string selectSql = @"
                            SELECT Role, IsActive
                            FROM dbo.Users WITH (UPDLOCK, HOLDLOCK)
                            WHERE UserID = @UserID";

                        string oldRole;
                        bool isActive;

                        using (SqlCommand cmd =
                            new SqlCommand(selectSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@UserID", SqlDbType.Int).Value = id;

                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Không tìm thấy tài khoản.");
                                }

                                oldRole = reader["Role"].ToString();
                                isActive = Convert.ToBoolean(reader["IsActive"]);
                            }
                        }

                        // Không hạ quyền Admin cuối cùng còn hoạt động
                        if (oldRole == "Admin"
                            && role == "Customer"
                            && isActive)
                        {
                            string countSql = @"
                                SELECT COUNT(*)
                                FROM dbo.Users
                                WHERE Role = N'Admin'
                                  AND IsActive = 1";

                            int activeAdmins;

                            using (SqlCommand cmd =
                                new SqlCommand(countSql, conn, transaction))
                            {
                                activeAdmins = Convert.ToInt32(cmd.ExecuteScalar());
                            }

                            if (activeAdmins <= 1)
                            {
                                throw new Exception(
                                    "Không thể hạ quyền Admin cuối cùng đang hoạt động.");
                            }
                        }

                        string updateSql = @"
                            UPDATE dbo.Users
                            SET Role = @Role
                            WHERE UserID = @UserID";

                        using (SqlCommand cmd =
                            new SqlCommand(updateSql, conn, transaction))
                        {
                            cmd.Parameters.Add("@Role",
                                SqlDbType.NVarChar, 20).Value = role;

                            cmd.Parameters.Add("@UserID",
                                SqlDbType.Int).Value = id;

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] =
                            "Đã cập nhật quyền tài khoản thành công.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        TempData["ErrorMessage"] = ex.Message;
                    }
                }
            }

            return RedirectToAction("Index");
        }
    }
}
