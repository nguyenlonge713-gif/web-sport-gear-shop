using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using web_sport_gear.Models;

namespace web_sport_gear.Areas.Admin.Controllers
{
    public class CategoriesController : Controller
    {
        // Chuỗi kết nối database
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["GearSportDB"]
                    .ConnectionString;
            }
        }

        // Kiểm tra quyền Admin
        private bool IsAdmin()
        {
            return Session["UserID"] != null &&
                   Session["Role"] != null &&
                   Session["Role"].ToString() == "Admin";
        }

        // Kiểm tra đăng nhập và quyền truy cập
        private ActionResult CheckAdmin()
        {
            if (Session["UserID"] == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account",
                    new { area = "" }
                );
            }

            if (!IsAdmin())
            {
                return RedirectToAction(
                    "Index",
                    "Home",
                    new { area = "" }
                );
            }

            return null;
        }

        // Trang mặc định
        public ActionResult Index()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return RedirectToAction("CategoryList");
        }

        // GET: Admin/Categories/CategoryList
        // Hiển thị danh sách danh mục
        public ActionResult CategoryList()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            List<Category> categories = new List<Category>();

            string sql = @"
                SELECT CategoryID, CategoryName
                FROM dbo.Categories
                ORDER BY CategoryID ASC";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        categories.Add(new Category
                        {
                            CategoryID = Convert.ToInt32(reader["CategoryID"]),
                            CategoryName = reader["CategoryName"].ToString()
                        });
                    }
                }
            }

            return View("CategoryList", categories);
        }

        // GET: Admin/Categories/CreateCategory
        public ActionResult CreateCategory()
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            return View("CreateCategory");
        }

        // POST: Admin/Categories/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateCategory(Category category)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Vui lòng nhập tên danh mục."
                );

                return View("CreateCategory", category);
            }

            string sql = @"
                INSERT INTO dbo.Categories (CategoryName)
                VALUES (@CategoryName)";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@CategoryName",
                    SqlDbType.NVarChar,
                    100
                ).Value = category.CategoryName.Trim();

                connection.Open();
                command.ExecuteNonQuery();
            }

            TempData["SuccessMessage"] =
                "Thêm danh mục thành công!";

            return RedirectToAction("CategoryList");
        }

        // GET: Admin/Categories/EditCategory/1
        public ActionResult EditCategory(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            Category category = null;

            string sql = @"
                SELECT CategoryID, CategoryName
                FROM dbo.Categories
                WHERE CategoryID = @CategoryID";

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        category = new Category
                        {
                            CategoryID = Convert.ToInt32(reader["CategoryID"]),
                            CategoryName = reader["CategoryName"].ToString()
                        };
                    }
                }
            }

            if (category == null)
                return HttpNotFound();

            return View("EditCategory", category);
        }

        // POST: Admin/Categories/EditCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCategory(Category category)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            if (string.IsNullOrWhiteSpace(category.CategoryName))
            {
                ModelState.AddModelError(
                    "CategoryName",
                    "Vui lòng nhập tên danh mục."
                );

                return View("EditCategory", category);
            }

            string sql = @"
                UPDATE dbo.Categories
                SET CategoryName = @CategoryName
                WHERE CategoryID = @CategoryID";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(sql, connection))
            {
                command.Parameters.Add(
                    "@CategoryName",
                    SqlDbType.NVarChar,
                    100
                ).Value = category.CategoryName.Trim();

                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = category.CategoryID;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
                return HttpNotFound();

            TempData["SuccessMessage"] =
                "Cập nhật danh mục thành công!";

            return RedirectToAction("CategoryList");
        }

        // POST: Admin/Categories/DeleteCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteCategory(int id)
        {
            ActionResult check = CheckAdmin();

            if (check != null)
                return check;

            // Kiểm tra danh mục còn sản phẩm hay không
            string checkSql = @"
                SELECT COUNT(*)
                FROM dbo.Products
                WHERE CategoryID = @CategoryID";

            int productCount;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(checkSql, connection))
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();
                productCount = Convert.ToInt32(command.ExecuteScalar());
            }

            // Không cho xóa nếu còn sản phẩm
            if (productCount > 0)
            {
                TempData["ErrorMessage"] =
                    "Không thể xóa danh mục này vì vẫn còn sản phẩm đang sử dụng!";

                return RedirectToAction("CategoryList");
            }

            // Xóa danh mục
            string deleteSql = @"
                DELETE FROM dbo.Categories
                WHERE CategoryID = @CategoryID";

            int rowsAffected;

            using (SqlConnection connection =
                new SqlConnection(ConnectionString))
            using (SqlCommand command =
                new SqlCommand(deleteSql, connection))
            {
                command.Parameters.Add(
                    "@CategoryID",
                    SqlDbType.Int
                ).Value = id;

                connection.Open();
                rowsAffected = command.ExecuteNonQuery();
            }

            if (rowsAffected == 0)
                return HttpNotFound();

            TempData["SuccessMessage"] =
                "Xóa danh mục thành công!";

            return RedirectToAction("CategoryList");
        }
    }
}