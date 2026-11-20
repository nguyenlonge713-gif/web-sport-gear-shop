
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class DashboardController : AdminBaseController
    {
        // Chuỗi kết nối SQL Server
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"].ConnectionString;
            }
        }

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

            // Khởi tạo giá trị mặc định
            decimal totalPaidRevenue = 0;
            int totalPaidOrders = 0;

            // Truy vấn thống kê đơn hàng đã thanh toán
            using (SqlConnection conn = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT
                        COUNT(*) AS TotalPaidOrders,
                        ISNULL(SUM(TotalAmount), 0) AS TotalPaidRevenue
                    FROM dbo.Orders
                    WHERE PaymentStatus = N'Đã thanh toán'
                      AND ISNULL(OrderStatus, N'') <> N'Đã hủy'";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    conn.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            totalPaidOrders = Convert.ToInt32(
                                reader["TotalPaidOrders"]
                            );

                            totalPaidRevenue = Convert.ToDecimal(
                                reader["TotalPaidRevenue"]
                            );
                        }
                    }
                }
            }

            // Truyền dữ liệu sang View
            ViewBag.TotalPaidRevenue = totalPaidRevenue;
            ViewBag.TotalPaidOrders = totalPaidOrders;

            return View();
        }
    }
}
