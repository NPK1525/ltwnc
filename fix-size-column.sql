-- Tăng kích thước cột Size và Color trong bảng Products
ALTER TABLE Products ALTER COLUMN Size NVARCHAR(500) NOT NULL;
ALTER TABLE Products ALTER COLUMN Color NVARCHAR(500) NOT NULL;
