USE GearSportDB;
GO

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
ORDER BY TABLE_NAME, ORDINAL_POSITION;

USE GearSportDB;
GO

UPDATE dbo.Users
SET Role = 'Admin'
WHERE UserID = 1
  AND Email = 'nguyenlonge713@gmail.com';

SELECT UserID, Email, Role, IsActive
FROM dbo.Users
WHERE UserID IN (1, 2);


USE GearSportDB;
GO

INSERT INTO dbo.Users
(
    FullName,
    Email,
    Phone,
    PasswordHash,
    Role,
    IsActive,
    CreatedAt
)
VALUES
(
    N'Administrator mới',
    'adminnew@gmail.com',
    '0999999999',
    N'ADLP0Zj4dtVNQh4jPoBQ3wIXGxmThVZEdmkBNVAxMvyQDZf3GUdwISErkBffCr8WpQ==',
    'Admin',
    1,
    GETDATE()
);
GO




SELECT
    UserID,
    FullName,
    Email,
    Phone,
    Role,
    IsActive,
    LEN(PasswordHash) AS PasswordHashLength
FROM dbo.Users
WHERE Email = 'adminnew@gmail.com';
