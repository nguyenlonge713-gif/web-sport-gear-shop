
using System.Web.Mvc;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class DashboardController : Controller
    {
        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return Session["UserID"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }

        // Trang tổng quan Admin
        public ActionResult Index()
        {
            // Chưa đăng nhập
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );
            }

            // Không có quyền Admin
            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "" }
                );
            }

            return View();
        }
    }
}
