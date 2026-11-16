
USE GearSportDB;
GO

-- 1. Tạo bảng danh mục
CREATE TABLE Categories
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(100) NOT NULL
);
GO

-- 2. Tạo bảng sản phẩm
CREATE TABLE Products
(
    ProductID INT IDENTITY(1,1) PRIMARY KEY,
    ProductName NVARCHAR(200) NOT NULL,
    Price DECIMAL(18,2) NOT NULL,
    ImageUrl NVARCHAR(1000),
    CategoryID INT NOT NULL,
    Description NVARCHAR(1000),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Products_Categories
        FOREIGN KEY (CategoryID)
        REFERENCES Categories(CategoryID),

    CONSTRAINT CK_Products_Price
        CHECK (Price >= 0)
);
GO

-- 3. Thêm danh mục
INSERT INTO Categories (CategoryName)
VALUES
(N'Áo thể thao'),
(N'Phụ kiện'),
(N'Giày thể thao');
GO

-- 4. Thêm 13 sản phẩm hiện có
-- Giữ nguyên ProductID để hạn chế ảnh hưởng đến giỏ hàng cũ.

SET IDENTITY_INSERT Products ON;

INSERT INTO Products
(ProductID, ProductName, Price, ImageUrl, CategoryID)
VALUES
(1, N'Giày Chạy Bộ Nike Air', 1500000,
 N'https://images.unsplash.com/photo-1542291026-7eec264c27ff', 3),

(2, N'Áo Thun Thể Thao Nam', 350000,
 N'https://images.unsplash.com/photo-1581655353564-df123a1eb820', 1),

(3, N'Quần Short Chạy Bộ Nam', 250000,
 N'https://images.unsplash.com/photo-1623285512357-ff3b9a7579ea?w=500&auto=format&fit=crop', 2),

(4, N'Bình Nước Thể Thao 1L', 180000,
 N'https://images.unsplash.com/photo-1602143407151-7111542de6e8', 2),

(5, N'Bóng Đá Tiêu Chuẩn FIFA', 650000,
 N'https://images.unsplash.com/photo-1518091043644-c1d4457512c6?q=80&w=1331&auto=format&fit=crop', 2),

(6, N'Găng Tay Thủ Môn Pro', 450000,
 N'https://plus.unsplash.com/premium_photo-1781205755096-6caab3faf2a1?q=80&w=986&auto=format&fit=crop', 2),

(9, N'Áo Khoác Gió Thể Thao Nam', 550000,
 N'https://images.unsplash.com/photo-1611308725032-74f0a551d018?q=80&w=722&auto=format&fit=crop', 1),

(10, N'Túi Trống Thể Thao Đa Năng', 390000,
 N'https://images.unsplash.com/photo-1553062407-98eeb64c6a62', 2),

(11, N'Vợt Cầu Lông Chuyên Nghiệp', 850000,
 N'https://images.unsplash.com/photo-1687597778602-624a9438fe0b?q=80&w=1935&auto=format&fit=crop', 2),

(15, N'Giày Bóng Rổ Nam Cao Cấp', 1650000,
 N'https://images.unsplash.com/photo-1579338559194-a162d19bf842', 3),

(16, N'Áo Polo Thể Thao Nam', 290000,
 N'https://plus.unsplash.com/premium_photo-1747861981617-1512bc2c06e8?q=80&w=687&auto=format&fit=crop', 1),

(17, N'Giày Đá Banh Sân Cỏ Nhân Tạo', 890000,
 N'https://images.unsplash.com/photo-1511886929837-354d827aae26?q=80&w=764&auto=format&fit=crop', 3),

(18, N'Giày Chạy Bộ Nam Lightweight', 1350000,
 N'https://images.unsplash.com/photo-1562183241-b937e95585b6?w=500&auto=format&fit=crop', 3);

SET IDENTITY_INSERT Products OFF;
GO

-- 5. Kiểm tra dữ liệu
SELECT * FROM Categories;

SELECT
    p.ProductID,
    p.ProductName,
    p.Price,
    c.CategoryName,
    p.ImageUrl,
    p.IsActive
FROM Products p
INNER JOIN Categories c
    ON p.CategoryID = c.CategoryID
ORDER BY p.ProductID;
GO


USE GearSportDB;
GO

SELECT UserID, FullName, Email, Role, IsActive
FROM dbo.Users
ORDER BY UserID;
UPDATE dbo.Users
SET Role = 'Admin'
WHERE UserID = 1;
SELECT UserID, FullName, Email, Role, IsActive
FROM dbo.Users
WHERE UserID = 1;


SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
ORDER BY TABLE_NAME, ORDINAL_POSITION;