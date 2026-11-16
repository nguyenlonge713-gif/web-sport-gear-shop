USE GearSportDB;
GO

-- Bảng lưu thông tin đơn hàng
CREATE TABLE dbo.Orders
(
    OrderID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT NOT NULL,

    ReceiverName NVARCHAR(100) NOT NULL,
    ReceiverPhone NVARCHAR(20) NOT NULL,
    ShippingAddress NVARCHAR(500) NOT NULL,

    TotalAmount DECIMAL(18,2) NOT NULL,

    PaymentMethod NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Orders_PaymentMethod DEFAULT N'COD',

    PaymentStatus NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Orders_PaymentStatus DEFAULT N'Chưa thanh toán',

    OrderStatus NVARCHAR(30) NOT NULL
        CONSTRAINT DF_Orders_OrderStatus DEFAULT N'Chờ xác nhận',

    Note NVARCHAR(500) NULL,

    CreatedAt DATETIME2 NOT NULL
        CONSTRAINT DF_Orders_CreatedAt DEFAULT SYSDATETIME(),

    CONSTRAINT FK_Orders_Users
        FOREIGN KEY (UserID)
        REFERENCES dbo.Users(UserID)
);
GO

-- Bảng lưu chi tiết sản phẩm trong đơn hàng
CREATE TABLE dbo.OrderDetails
(
    OrderDetailID INT IDENTITY(1,1) PRIMARY KEY,

    OrderID INT NOT NULL,
    ProductID INT NOT NULL,

    ProductName NVARCHAR(200) NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,

    CONSTRAINT FK_OrderDetails_Orders
        FOREIGN KEY (OrderID)
        REFERENCES dbo.Orders(OrderID),

    CONSTRAINT FK_OrderDetails_Products
        FOREIGN KEY (ProductID)
        REFERENCES dbo.Products(ProductID),

    CONSTRAINT CK_OrderDetails_Quantity
        CHECK (Quantity > 0),

    CONSTRAINT CK_OrderDetails_UnitPrice
        CHECK (UnitPrice >= 0)
);
GO


SELECT TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Orders', 'OrderDetails');


USE GearSportDB;

SELECT * FROM dbo.Orders
ORDER BY OrderID DESC;

SELECT * FROM dbo.OrderDetails
ORDER BY OrderDetailID DESC;

SELECT DB_NAME() AS CurrentDatabase;
USE GearSportDB;
GO

SELECT OrderID, OrderStatus
FROM dbo.Orders
WHERE OrderID = 1;



SELECT
    ProductID,
    ProductName,
    StockQuantity,
    IsActive
FROM dbo.Products
ORDER BY ProductID;


SELECT
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Users'
ORDER BY ORDINAL_POSITION;

ALTER TABLE dbo.Users
ADD
    DateOfBirth DATE NULL,
    Gender NVARCHAR(20) NULL,
    Address NVARCHAR(255) NULL,
    Avatar NVARCHAR(500) NULL;


    SELECT TOP 10 *
FROM dbo.Users;


USE GearSportDB;

EXEC sp_help 'dbo.Orders';

EXEC sp_help 'dbo.OrderDetails';


EXEC sp_help 'dbo.OrderDetails';

SELECT name
FROM sys.databases
WHERE database_id > 4;

USE GearSportDB;
GO

SELECT DB_NAME() AS CurrentDatabase;

SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME IN ('Orders', 'OrderDetails');


USE GearSportDB;
GO

EXEC sp_help 'dbo.Orders';
EXEC sp_help 'dbo.OrderDetails';

USE GearSportDB;
GO

EXEC sp_help 'dbo.OrderDetails';




USE GearSportDB;
GO

-- Kiểm tra khóa chính
SELECT
    tc.TABLE_NAME,
    kcu.COLUMN_NAME,
    tc.CONSTRAINT_TYPE,
    tc.CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
    ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
WHERE tc.TABLE_NAME IN ('Orders', 'OrderDetails')
  AND tc.CONSTRAINT_TYPE IN ('PRIMARY KEY', 'FOREIGN KEY');

-- Kiểm tra các cột IDENTITY
SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMNPROPERTY(
        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
        COLUMN_NAME,
        'IsIdentity'
    ) AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'OrderDetails')
  AND COLUMN_NAME IN ('OrderID', 'OrderDetailID');





  USE GearSportDB;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMNPROPERTY(
        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
        COLUMN_NAME,
        'IsIdentity'
    ) AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'OrderDetails')
  AND COLUMN_NAME IN ('OrderID', 'OrderDetailID');

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN ('Orders', 'OrderDetails');






USE GearSportDB;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMNPROPERTY(
        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
        COLUMN_NAME,
        'IsIdentity'
    ) AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'OrderDetails')
  AND COLUMN_NAME IN ('OrderID', 'OrderDetailID');

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN ('Orders', 'OrderDetails');



USE GearSportDB;
GO

SELECT
    COLUMN_NAME AS TenCot,
    DATA_TYPE AS KieuDuLieu,
    CHARACTER_MAXIMUM_LENGTH AS DoDai,
    IS_NULLABLE AS ChoPhepNull
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products'
ORDER BY ORDINAL_POSITION;


USE GearSportDB;
GO

SELECT
    TABLE_NAME,
    COLUMN_NAME,
    COLUMNPROPERTY(
        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
        COLUMN_NAME,
        'IsIdentity'
    ) AS IsIdentity
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'OrderDetails')
  AND COLUMN_NAME IN ('OrderID', 'OrderDetailID');

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) IN ('Orders', 'OrderDetails');


USE GearSportDB;
GO

SELECT
    c.name AS ColumnName,
    dc.definition AS DefaultValue
FROM sys.columns c
LEFT JOIN sys.default_constraints dc
    ON c.default_object_id = dc.object_id
WHERE c.object_id = OBJECT_ID('dbo.Orders')
  AND c.name = 'CreatedAt';




  USE GearSportDB;
GO

SELECT
    TABLE_NAME AS TenBang,
    COLUMN_NAME AS TenCot,
    COLUMNPROPERTY(
        OBJECT_ID(TABLE_SCHEMA + '.' + TABLE_NAME),
        COLUMN_NAME,
        'IsIdentity'
    ) AS TuDongTang
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME IN ('Orders', 'OrderDetails')
  AND COLUMN_NAME IN ('OrderID', 'OrderDetailID');

SELECT
    OD.OrderDetailID,
    OD.OrderID,
    OD.ProductID,
    OD.ProductName,
    OD.Quantity,
    OD.UnitPrice,
    OD.Quantity * OD.UnitPrice AS SubTotal
FROM dbo.OrderDetails OD
WHERE OD.OrderID = 5;

SELECT DB_NAME() AS CurrentDatabase;
USE [GearSportDB];
GO

SELECT DB_NAME() AS CurrentDatabase;
SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'Orders';
SELECT
    COLUMN_NAME,
    DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Orders'
ORDER BY ORDINAL_POSITION;
-- Thêm cột đánh dấu đã trừ kho
IF COL_LENGTH('dbo.Orders', 'StockDeducted') IS NULL
BEGIN
    ALTER TABLE dbo.Orders
    ADD StockDeducted BIT NOT NULL
        CONSTRAINT DF_Orders_StockDeducted DEFAULT 0;
END;
GO

-- Đánh dấu các đơn hàng cũ đã trừ kho
-- Vì CheckoutController phiên bản cũ trừ kho ngay khi đặt hàng.
-- Các đơn đã hủy được xem là đã hoàn kho.
UPDATE dbo.Orders
SET StockDeducted =
    CASE
        WHEN OrderStatus = N'Đã hủy' THEN 0
        ELSE 1
    END;
GO

SELECT TOP 10
    OrderID,
    UserID,
    TotalAmount,
    OrderStatus,
    CreatedAt
FROM Orders
WHERE UserID = 2
ORDER BY OrderID DESC;

SELECT
    o.OrderID,
    o.OrderStatus,
    o.StockDeducted,
    p.ProductID,
    p.ProductName,
    p.StockQuantity
FROM dbo.Orders o
INNER JOIN dbo.OrderDetails d
    ON o.OrderID = d.OrderID
INNER JOIN dbo.Products p
    ON d.ProductID = p.ProductID
WHERE o.OrderID = 7;