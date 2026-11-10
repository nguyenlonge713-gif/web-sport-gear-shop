
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

            // 1. Chưa đăng nhập: chuyển đến trang đăng nhập.
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
                int userId = Convert.ToInt32(Session["UserID"]);

                string connectionString =
                    ConfigurationManager
                    .ConnectionStrings["GearSportDB"]
                    .ConnectionString;

                // 2. Kiểm tra quyền và trạng thái tài khoản trực tiếp từ database.
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    string sql = @"
                        SELECT Role, IsActive
                        FROM dbo.Users
                        WHERE UserID = @UserID";

                    using (SqlCommand cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.Add("@UserID", SqlDbType.Int)
                            .Value = userId;

                        conn.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            // Tài khoản không tồn tại hoặc đã bị vô hiệu hóa.
                            if (!reader.Read() ||
                                !Convert.ToBoolean(reader["IsActive"]))
                            {
                                ClearUserSession();

                                filterContext.Result = RedirectToAction(
                                    "Index",
                                    "Home",
                                    new { area = "" }
                                );

                                return;
                            }

                            string currentRole =
                                Convert.ToString(reader["Role"]);

                            // Tài khoản không còn quyền Admin.
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

                            // Đồng bộ lại quyền hiện tại vào Session.
                            Session["Role"] = currentRole;
                        }
                    }
                }
            }
            catch
            {
                // Nếu không xác minh được quyền từ database,
                // mặc định từ chối truy cập Admin.
                ClearUserSession();

                filterContext.Result = RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );

                return;
            }
        }

        private void ClearUserSession()
        {
            Session.Clear();
            Session.Abandon();
        }
    }
}
