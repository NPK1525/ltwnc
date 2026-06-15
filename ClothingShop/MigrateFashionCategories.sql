-- Script để migrate data từ FashionCategories sang ProductCategories
-- Chạy script này trong SQL Server Management Studio hoặc Azure Data Studio

-- Bước 1: Insert data từ FashionCategories vào ProductCategories (nếu chưa tồn tại)
INSERT INTO ProductCategories (Name, Description, DisplayOrder, IsActive, BannerImageUrl, BannerImageUrlAvif, LinkUrl, IsFeatured, CreatedAt)
SELECT 
    Title AS Name,
    NULL AS Description,
    DisplayOrder,
    IsActive,
    ImageUrl AS BannerImageUrl,
    ImageUrlAvif AS BannerImageUrlAvif,
    LinkUrl,
    1 AS IsFeatured, -- Đánh dấu là featured vì đang hiển thị trên trang chủ
    CreatedAt
FROM FashionCategories
WHERE NOT EXISTS (
    SELECT 1 FROM ProductCategories WHERE Name = FashionCategories.Title
);

-- Bước 2: Kiểm tra kết quả
SELECT 
    Id,
    Name,
    BannerImageUrl,
    IsFeatured,
    DisplayOrder,
    IsActive
FROM ProductCategories
WHERE IsFeatured = 1
ORDER BY DisplayOrder;

-- Bước 3: (Tùy chọn) Xóa bảng FashionCategories sau khi đã migrate xong
-- DROP TABLE FashionCategories;
