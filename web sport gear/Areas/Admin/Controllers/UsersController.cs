
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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

        // DANH SACH TAI KHOAN CUSTOMER - CHI XEM
        public ActionResult Index(string keyword)
        {
            List<AdminUserViewModel> users =
                new List<AdminUserViewModel>();

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
                WHERE Role = N'Customer'";

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += @"
                    AND (
                        FullName LIKE @Keyword
                        OR Email LIKE @Keyword
                        OR Phone LIKE @Keyword
                    )";
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

            return View(users);
        }
    }
}
