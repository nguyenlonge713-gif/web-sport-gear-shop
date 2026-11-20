
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace web_sport_gear.Controllers
{
    public class CartController : Controller
    {
        private readonly string connectionString =
            ConfigurationManager.ConnectionStrings["GearSportDB"].ConnectionString;

        // Kiểm tra tài khoản khách hàng đã đăng nhập
        private bool TryGetCustomerId(out int userId)
        {
            userId = 0;

            if (Session["UserID"] == null ||
                Session["Role"] == null ||
                !string.Equals(
                    Session["Role"].ToString(),
                    "Customer",
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return int.TryParse(Session["UserID"].ToString(), out userId)
                   && userId > 0;
        }

        // Trả về thông báo lỗi JSON
        private JsonResult Error(string message)
        {
            return Json(new
            {
                success = false,
                message = message
            });
        }

        // Thêm sản phẩm vào giỏ hàng
        // Tồn kho được trừ ngay khi thêm sản phẩm
        [HttpPost]
        public ActionResult Add(int productId, int quantity = 1)
        {
            int userId;

            if (!TryGetCustomerId(out userId))
            {
                return Error(
                    "Vui lòng đăng nhập bằng tài khoản khách hàng.");
            }

            if (productId <= 0 || quantity <= 0 || quantity > 100)
            {
                return Error(
                    "Thông tin sản phẩm hoặc số lượng không hợp lệ.");
            }

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlTransaction tran =
                        conn.BeginTransaction())
                    {
                        try
                        {
                            string name;
                            decimal price;
                            string image;
                            int stock;
                            int oldQuantity = 0;

                            // Lấy thông tin sản phẩm và khóa dòng tồn kho
                            using (SqlCommand cmd = new SqlCommand(@"
                                SELECT
                                    ProductName,
                                    Price,
                                    ImageUrl,
                                    StockQuantity
                                FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
                                WHERE ProductID = @ProductID
                                  AND IsActive = 1;",
                                conn, tran))
                            {
                                cmd.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int
                                ).Value = productId;

                                using (SqlDataReader reader =
                                    cmd.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new Exception(
                                            "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh.");
                                    }

                                    name = reader["ProductName"].ToString();

                                    price = Convert.ToDecimal(
                                        reader["Price"]);

                                    image = reader["ImageUrl"] == DBNull.Value
                                        ? ""
                                        : reader["ImageUrl"].ToString();

                                    stock = Convert.ToInt32(
                                        reader["StockQuantity"]);
                                }
                            }

                            // Lấy số lượng sản phẩm khách đã giữ trong giỏ
                            using (SqlCommand cmd = new SqlCommand(@"
                                SELECT Quantity
                                FROM dbo.CartReservations WITH (UPDLOCK, HOLDLOCK)
                                WHERE UserID = @UserID
                                  AND ProductID = @ProductID;",
                                conn, tran))
                            {
                                cmd.Parameters.Add(
                                    "@UserID",
                                    SqlDbType.Int
                                ).Value = userId;

                                cmd.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int
                                ).Value = productId;

                                object result = cmd.ExecuteScalar();

                                if (result != null)
                                {
                                    oldQuantity =
                                        Convert.ToInt32(result);
                                }
                            }

                            // Tính số lượng mới
                            int newQuantity = oldQuantity + quantity;

                            // Số lượng cần trừ thêm khỏi kho
                            int additionalQuantity =
                                newQuantity - oldQuantity;

                            if (stock < additionalQuantity)
                            {
                                throw new Exception(
                                    "Số lượng tồn kho không đủ.");
                            }

                            // Trừ kho và lưu số lượng khách đang giữ
                            using (SqlCommand cmd = new SqlCommand(@"
                                UPDATE dbo.Products
                                SET StockQuantity =
                                    StockQuantity - @Quantity
                                WHERE ProductID = @ProductID;

                                IF EXISTS
                                (
                                    SELECT 1
                                    FROM dbo.CartReservations
                                    WHERE UserID = @UserID
                                      AND ProductID = @ProductID
                                )
                                BEGIN
                                    UPDATE dbo.CartReservations
                                    SET Quantity = @NewQuantity
                                    WHERE UserID = @UserID
                                      AND ProductID = @ProductID;
                                END
                                ELSE
                                BEGIN
                                    INSERT INTO dbo.CartReservations
                                        (UserID, ProductID, Quantity)
                                    VALUES
                                        (@UserID, @ProductID, @NewQuantity);
                                END;",
                                conn, tran))
                            {
                                cmd.Parameters.Add(
                                    "@Quantity",
                                    SqlDbType.Int
                                ).Value = additionalQuantity;

                                cmd.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int
                                ).Value = productId;

                                cmd.Parameters.Add(
                                    "@UserID",
                                    SqlDbType.Int
                                ).Value = userId;

                                cmd.Parameters.Add(
                                    "@NewQuantity",
                                    SqlDbType.Int
                                ).Value = newQuantity;

                                cmd.ExecuteNonQuery();
                            }

                            tran.Commit();

                            return Json(new
                            {
                                success = true,
                                message = "Đã thêm sản phẩm vào giỏ hàng.",
                                item = new
                                {
                                    id = productId,
                                    name = name,
                                    price = price,
                                    image = image,
                                    quantity = newQuantity,
                                    stock = stock - additionalQuantity
                                }
                            });
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // Cập nhật số lượng sản phẩm trong giỏ
        [HttpPost]
        public ActionResult UpdateQuantity(
            int productId,
            int quantity)
        {
            int userId;

            if (!TryGetCustomerId(out userId))
            {
                return Error(
                    "Vui lòng đăng nhập bằng tài khoản khách hàng.");
            }

            if (productId <= 0 || quantity < 0 || quantity > 100)
            {
                return Error("Số lượng không hợp lệ.");
            }

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    conn.Open();

                    using (SqlTransaction tran =
                        conn.BeginTransaction())
                    {
                        try
                        {
                            int oldQuantity;
                            int stock;

                            // Lấy số lượng đang giữ
                            using (SqlCommand cmd = new SqlCommand(@"
                                SELECT Quantity
                                FROM dbo.CartReservations WITH (UPDLOCK, HOLDLOCK)
                                WHERE UserID = @UserID
                                  AND ProductID = @ProductID;",
                                conn, tran))
                            {
                                cmd.Parameters.Add(
                                    "@UserID",
                                    SqlDbType.Int
                                ).Value = userId;

                                cmd.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int
                                ).Value = productId;

                                object result = cmd.ExecuteScalar();

                                if (result == null)
                                {
                                    throw new Exception(
                                        "Sản phẩm không còn trong giỏ hàng.");
                                }

                                oldQuantity =
                                    Convert.ToInt32(result);
                            }

                            // Lấy tồn kho hiện tại
                            using (SqlCommand cmd = new SqlCommand(@"
                                SELECT StockQuantity
                                FROM dbo.Products WITH (UPDLOCK, HOLDLOCK)
                                WHERE ProductID = @ProductID;",
                                conn, tran))
                            {
                                cmd.Parameters.Add(
                                    "@ProductID",
                                    SqlDbType.Int
                                ).Value = productId;

                                object result = cmd.ExecuteScalar();

                                if (result == null ||
                                    result == DBNull.Value)
                                {
                                    throw new Exception(
                                        "Không tìm thấy sản phẩm trong kho.");
                                }

                                stock = Convert.ToInt32(result);
                            }

                            // Số lượng thay đổi
                            int difference = quantity - oldQuantity;

                            // Nếu tăng số lượng thì kiểm tra tồn kho
                            if (difference > 0 && stock < difference)
                            {
                                throw new Exception(
                                    "Số lượng tồn kho không đủ.");
                            }

                            // Nếu số lượng bằng 0: xóa khỏi giỏ và hoàn kho
                            if (quantity == 0)
                            {
                                using (SqlCommand cmd = new SqlCommand(@"
                                    UPDATE dbo.Products
                                    SET StockQuantity =
                                        StockQuantity + @OldQuantity
                                    WHERE ProductID = @ProductID;

                                    DELETE FROM dbo.CartReservations
                                    WHERE UserID = @UserID
                                      AND ProductID = @ProductID;",
                                    conn, tran))
                                {
                                    cmd.Parameters.Add(
                                        "@OldQuantity",
                                        SqlDbType.Int
                                    ).Value = oldQuantity;

                                    cmd.Parameters.Add(
                                        "@ProductID",
                                        SqlDbType.Int
                                    ).Value = productId;

                                    cmd.Parameters.Add(
                                        "@UserID",
                                        SqlDbType.Int
                                    ).Value = userId;

                                    cmd.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                // Nếu tăng: trừ thêm
                                // Nếu giảm: hoàn lại phần chênh lệch
                                using (SqlCommand cmd = new SqlCommand(@"
                                    UPDATE dbo.Products
                                    SET StockQuantity =
                                        StockQuantity - @Difference
                                    WHERE ProductID = @ProductID;

                                    UPDATE dbo.CartReservations
                                    SET Quantity = @Quantity
                                    WHERE UserID = @UserID
                                      AND ProductID = @ProductID;",
                                    conn, tran))
                                {
                                    cmd.Parameters.Add(
                                        "@Difference",
                                        SqlDbType.Int
                                    ).Value = difference;

                                    cmd.Parameters.Add(
                                        "@ProductID",
                                        SqlDbType.Int
                                    ).Value = productId;

                                    cmd.Parameters.Add(
                                        "@Quantity",
                                        SqlDbType.Int
                                    ).Value = quantity;

                                    cmd.Parameters.Add(
                                        "@UserID",
                                        SqlDbType.Int
                                    ).Value = userId;

                                    cmd.ExecuteNonQuery();
                                }
                            }

                            tran.Commit();

                            return Json(new
                            {
                                success = true,
                                message = "Đã cập nhật giỏ hàng.",
                                quantity = quantity,
                                stock = stock - difference
                            });
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }

        // Xóa sản phẩm khỏi giỏ và hoàn lại tồn kho
        [HttpPost]
        public ActionResult Remove(int productId)
        {
            return UpdateQuantity(productId, 0);
        }

        // Lấy giỏ hàng từ SQL Server
        [HttpGet]
        public ActionResult GetCart()
        {
            int userId;

            if (!TryGetCustomerId(out userId))
            {
                return Error(
                    "Vui lòng đăng nhập bằng tài khoản khách hàng.");
            }

            try
            {
                using (SqlConnection conn =
                    new SqlConnection(connectionString))
                {
                    conn.Open();

                    var items = new List<object>();

                    using (SqlCommand cmd = new SqlCommand(@"
                        SELECT
                            r.ProductID,
                            r.Quantity,
                            p.ProductName,
                            p.Price,
                            p.ImageUrl,
                            p.StockQuantity
                        FROM dbo.CartReservations r
                        INNER JOIN dbo.Products p
                            ON r.ProductID = p.ProductID
                        WHERE r.UserID = @UserID
                        ORDER BY r.ProductID;",
                        conn))
                    {
                        cmd.Parameters.Add(
                            "@UserID",
                            SqlDbType.Int
                        ).Value = userId;

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                items.Add(new
                                {
                                    id = Convert.ToInt32(
                                        reader["ProductID"]),

                                    name = reader["ProductName"].ToString(),

                                    price = Convert.ToDecimal(
                                        reader["Price"]),

                                    image = reader["ImageUrl"] == DBNull.Value
                                        ? ""
                                        : reader["ImageUrl"].ToString(),

                                    quantity = Convert.ToInt32(
                                        reader["Quantity"]),

                                    stock = Convert.ToInt32(
                                        reader["StockQuantity"])
                                });
                            }
                        }
                    }

                    return Json(new
                    {
                        success = true,
                        items = items
                    }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Error(ex.Message);
            }
        }
    }
}
