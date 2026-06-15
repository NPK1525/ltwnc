# Hướng Dẫn Cấu Hình & Cài Đặt (Setup) Dự Án ClothingShop

Tài liệu này hướng dẫn chi tiết cách cài đặt môi trường, cấu hình cơ sở dữ liệu và khởi chạy ứng dụng **ClothingShop** trên máy tính cá nhân.

---

## 1. Chuẩn Bị Môi Trường (Prerequisites)

Trước khi bắt đầu, hãy đảm bảo máy tính của bạn đã cài đặt các công cụ sau:

1. **.NET 10 SDK:**
   * Tải về và cài đặt phiên bản .NET SDK tương ứng từ [Trang chủ Microsoft .NET](https://dotnet.microsoft.com/download).
   * Kiểm tra phiên bản bằng lệnh: `dotnet --version` (yêu cầu .NET 10.0+ để tương thích với cấu hình trong [ClothingShop.csproj](file:///d:/tmdt/ClothingShop/ClothingShop.csproj)).

2. **Hệ Quản Trị Cơ Sở Dữ Liệu SQL Server:**
   * Khuyên dùng **SQL Server LocalDB** (được cài đặt sẵn kèm theo Visual Studio).
   * Hoặc bạn có thể sử dụng các phiên bản như **SQL Server Express** hay **SQL Server Developer Edition**.

3. **Công Cụ Phát Triển (IDE):**
   * **Visual Studio 2022** (hỗ trợ tốt nhất cho lập trình C#/.NET Core).
   * Hoặc **Visual Studio Code** (cần cài đặt thêm bộ Extension *C# Dev Kit* và *SQL Server*).

---

## 2. Cấu Hình Chuỗi Kết Nối Cơ Sở Dữ Liệu (Database Connection)

Mở tệp cấu hình chính [appsettings.json](file:///d:/tmdt/ClothingShop/appsettings.json) tại thư mục dự án và tìm mục `ConnectionStrings`:

*   **Nếu sử dụng SQL Server LocalDB mặc định (Visual Studio):**
    Giữ nguyên cấu hình ban đầu:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ClothingShop;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```

*   **Nếu sử dụng SQL Server phiên bản độc lập (Đăng nhập qua Account):**
    Thay thế chuỗi kết nối bằng cấu hình sau:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=TÊN_MÁY_CHỦ;Database=ClothingShop;User Id=TÀI_KHOẢN_SA;Password=MẬT_KHẨU;TrustServerCertificate=True;MultipleActiveResultSets=true"
    }
    ```

---

## 3. Khởi Tạo Cơ Sở Dữ Liệu & Dữ Liệu Mẫu (Database & Seeding)

Dự án đã tích hợp tính năng **Tự động áp dụng Migration và Seed Dữ liệu** khi khởi chạy ứng dụng (Xem chi tiết tại [Program.cs:L68](file:///d:/tmdt/ClothingShop/Program.cs#L68)). Do đó, hệ thống sẽ tự động tạo cơ sở dữ liệu `ClothingShop` và nạp dữ liệu mẫu khi bạn chạy dự án lần đầu tiên.

### Dữ liệu mẫu tự động được nạp bao gồm:
*   **Tài khoản Quản trị viên (Admin):**
    *   **Email:** `gamer957ola@gmail.com`
    *   **Mật khẩu:** `Khang@123`
*   **Các Voucher khuyến mại mẫu:** `SUMMER20` và `FREESHIP` để kiểm thử giỏ hàng.
*   **Biến thể sản phẩm:** Tự động đồng bộ các size/màu và tạo các bản ghi lô hàng tồn kho.

*Lưu ý:* Nếu bạn muốn áp dụng các bản cập nhật database thủ công từ Command Line (không thông qua cơ chế tự động chạy), hãy mở Terminal tại thư mục `ClothingShop` và chạy:
```bash
dotnet ef database update
```

---

## 4. Hướng Dẫn Khởi Chạy Dự Án (Running)

### Cách 1: Sử dụng Visual Studio 2022
1.  Nhấp đúp chuột để mở tệp giải pháp [ClothingShop.sln](file:///d:/tmdt/ClothingShop.sln).
2.  Chờ Visual Studio tự động khôi phục các gói thư viện NuGet (Restore NuGet Packages).
3.  Nhấn nút **Start (F5)** hoặc nhấp chuột vào biểu tượng mũi tên xanh trên thanh công cụ để khởi chạy dự án.

### Cách 2: Sử dụng Command Line (Terminal / VS Code)
1.  Mở Terminal tại thư mục `ClothingShop` của dự án:
    ```bash
    cd d:\tmdt\ClothingShop
    ```
2.  Khởi động ứng dụng bằng lệnh:
    ```bash
    dotnet run
    ```
3.  Truy cập vào ứng dụng qua trình duyệt theo địa chỉ cổng (Port) hiển thị trong Terminal (Ví dụ: `http://localhost:5000` hoặc `https://localhost:5001`).

---

## 5. Cấu Hình Bổ Sung (Nâng Cao)

*   **Tích hợp cổng VNPay (Thanh toán trực tuyến):**
    Bạn có thể đăng ký tài khoản thử nghiệm tại trang Sandbox của VNPay và điền thông tin cổng kết nối tại [appsettings.json:L13](file:///d:/tmdt/ClothingShop/appsettings.json#L13):
    ```json
    "VNPay": {
      "TmnCode": "MÃ_TMN_CODE_CỦA_BẠN",
      "HashSecret": "CHUỖI_MẬT_MÃ_BẢO_MẬT_VNPAY",
      "Url": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
    }
    ```
*   **Thông tin chuyển khoản ngân hàng thủ công:**
    Bạn có thể cấu hình tài khoản ngân hàng hiển thị ở trang checkout trong mục `PaymentInfo` của [appsettings.json](file:///d:/tmdt/ClothingShop/appsettings.json).
