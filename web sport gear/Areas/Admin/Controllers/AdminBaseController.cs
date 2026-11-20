
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class AdminBaseController : Controller
    {
        protected override void OnActionExecuting(
            ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            // 1. Kiểm tra đăng nhập
            if (Session["UserID"] == null)
            {
                filterContext.Result = RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );

                return;
            }

            try
            {
                // 2. Kiểm tra UserID trong Session
                int userId;

                if (!int.TryParse(
                    Convert.ToString(Session["UserID"]),
                    out userId))
                {
                    ClearUserSession();

                    filterContext.Result = RedirectToAction(
                        "Login",
                        "Account",
                        new { area = "" }
                    );

                    return;
                }

                string connectionString =
                    ConfigurationManager
                    .ConnectionStrings["GearSportDB"]
                    .ConnectionString;

                // 3. Kiểm tra quyền trực tiếp từ database
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    string sql = @"
                        SELECT Role, IsActive
                        FROM dbo.Users
                        WHERE UserID = @UserID;";

                    using (SqlCommand cmd =
                        new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = userId;

                        conn.Open();

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            // Tài khoản không tồn tại
                            if (!reader.Read())
                            {
                                ClearUserSession();

                                filterContext.Result = RedirectToAction(
                                    "Login",
                                    "Account",
                                    new { area = "" }
                                );

                                return;
                            }

                            // Tài khoản bị vô hiệu hóa
                            if (reader["IsActive"] == DBNull.Value ||
                                !Convert.ToBoolean(reader["IsActive"]))
                            {
                                ClearUserSession();

                                filterContext.Result = RedirectToAction(
                                    "Login",
                                    "Account",
                                    new { area = "" }
                                );

                                return;
                            }

                            string currentRole =
                                Convert.ToString(reader["Role"]);

                            // Chỉ Admin mới được truy cập khu vực Admin
                            if (!string.Equals(
                                currentRole,
                                "Admin",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                ClearUserSession();

                                filterContext.Result = RedirectToAction(
                                    "Index",
                                    "Home",
                                    new { area = "" }
                                );

                                return;
                            }

                            // Đồng bộ quyền hiện tại vào Session
                            Session["Role"] = currentRole;
                        }
                    }
                }
            }
            catch
            {
                // Không xác minh được quyền thì từ chối truy cập
                ClearUserSession();

                filterContext.Result = RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );

                return;
            }
        }

        // Xóa thông tin đăng nhập
        private void ClearUserSession()
        {
            Session.Clear();
            Session.Abandon();
        }
    }
}
