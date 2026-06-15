using ClothingShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ClothingShop.Controllers.Admin
{
    [Authorize(Policy = "AdminOnly")]
    [Route("Admin/Payment")]
    public class AdminPaymentController(IConfiguration configuration, IWebHostEnvironment env) : Controller
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IWebHostEnvironment _env = env;

        // Cache JsonSerializerOptions để tránh tạo mới mỗi lần
        private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

        // GET: /Admin/Payment
        [HttpGet("")]
        public IActionResult Index()
        {
            var paymentInfo = new PaymentInfoViewModel
            {
                BankName = _configuration["PaymentInfo:BankName"] ?? "Vietcombank",
                BankAccountNumber = _configuration["PaymentInfo:BankAccountNumber"] ?? "1234567890",
                BankAccountName = _configuration["PaymentInfo:BankAccountName"] ?? "NGUYEN VAN A",
                MoMoPhone = _configuration["PaymentInfo:MoMoPhone"] ?? "0901234567",
                MoMoName = _configuration["PaymentInfo:MoMoName"] ?? "NGUYEN VAN A"
            };

            return View("~/Views/Admin/PaymentSettings.cshtml", paymentInfo);
        }

        // POST: /Admin/Payment/Update
        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public IActionResult Update(PaymentInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/Admin/PaymentSettings.cshtml", model);
            }

            try
            {
                // Đọc appsettings.json
                var appSettingsPath = Path.Combine(_env.ContentRootPath, "appsettings.json");
                var json = System.IO.File.ReadAllText(appSettingsPath);
                var jsonObj = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (jsonObj != null)
                {
                    // Cập nhật PaymentInfo
                    var paymentInfo = new Dictionary<string, string>
                    {
                        ["BankName"] = model.BankName,
                        ["BankAccountNumber"] = model.BankAccountNumber,
                        ["BankAccountName"] = model.BankAccountName,
                        ["MoMoPhone"] = model.MoMoPhone,
                        ["MoMoName"] = model.MoMoName
                    };

                    jsonObj["PaymentInfo"] = paymentInfo;

                    // Ghi lại file
                    var updatedJson = JsonSerializer.Serialize(jsonObj, _jsonOptions);
                    System.IO.File.WriteAllText(appSettingsPath, updatedJson);

                    TempData["Success"] = "Cập nhật thông tin thanh toán thành công! Vui lòng restart ứng dụng để áp dụng thay đổi.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi khi cập nhật: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
