-- Script để kiểm tra và cập nhật độ dài cột Size và Color trong bảng Products

-- Kiểm tra độ dài hiện tại
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Products' 
AND COLUMN_NAME IN ('Size', 'Color');

-- Nếu độ dài < 500, chạy lệnh ALTER TABLE sau:
-- ALTER TABLE Products ALTER COLUMN Size NVARCHAR(500) NOT NULL;
-- ALTER TABLE Products ALTER COLUMN Color NVARCHAR(500) NOT NULL;
