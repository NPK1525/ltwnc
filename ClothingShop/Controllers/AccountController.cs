using ClothingShop.Data;
using ClothingShop.Models;
using ClothingShop.Services;
using ClothingShop.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace ClothingShop.Controllers
{
    public partial class AccountController(ApplicationDbContext context, IEmailService emailService) : Controller
    {
        private readonly ApplicationDbContext _context = context;
        private readonly IEmailService _emailService = emailService;

        // ✅ COMPILED REGEX for better performance
        [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"[A-Z]")]
        private static partial Regex UppercaseRegex();

        [GeneratedRegex(@"[@$!%*?&#]")]
        private static partial Regex SpecialCharRegex();

        [GeneratedRegex(@"^(0|\+84)[0-9]{9,10}$")]
        private static partial Regex PhoneRegex();

        /// <summary>Kiểm tra mật khẩu đủ mạnh: độ dài, chữ hoa, ký tự đặc biệt.</summary>
        private static bool IsStrongPassword(string password)
            => password.Length >= Constants.Auth.PasswordMinLength
            && UppercaseRegex().IsMatch(password)
            && SpecialCharRegex().IsMatch(password);

        // [GET] /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // [POST] /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string Email, string Password, string? ReturnUrl)
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                TempData["Error"] = "Vui lòng nhập email và mật khẩu!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            Email = Email.Trim().ToLower();
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == Email);

            if (user == null || !VerifyPassword(Password, user.PasswordHash, out bool isOldHash))
            {
                TempData["Error"] = "Email hoặc mật khẩu không đúng!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (isOldHash)
            {
                user.PasswordHash = HashPassword(Password);
                await _context.SaveChangesAsync();
            }

            // Kiểm tra tài khoản có bị khóa không (Admin luôn được phép đăng nhập)
            if (!user.IsActive && !user.IsAdmin)
            {
                TempData["Error"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên để biết thêm chi tiết.";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            // TẠO CLAIMS
            var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new(ClaimTypes.Name, user.FullName ?? user.Email),
                    new(ClaimTypes.Email, user.Email)
                };

            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // ĐĂNG NHẬP THỰC SỰ
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            // LƯU VÀO SESSION
            HttpContext.Session.SetString(Constants.SessionKeys.UserId, user.Id.ToString());
            HttpContext.Session.SetString(Constants.SessionKeys.UserName, user.FullName ?? user.Email);
            HttpContext.Session.SetString(Constants.SessionKeys.IsAdmin, user.IsAdmin.ToString());

            if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            {
                return Redirect(ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        // [GET] /Account/Register
        [HttpGet]
        public IActionResult Register(string? returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // [POST] /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string FullName, string Email, string Password, string ConfirmPassword, string PhoneNumber, string? Gender, string? ReturnUrl)
        {
            // ✅ VALIDATION CƠ BẢN
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            // ✅ VALIDATION ĐỘ DÀI
            if (FullName.Length < 2 || FullName.Length > Constants.StringLength.NameMaxLength)
            {
                TempData["Error"] = $"Họ tên phải từ 2-{Constants.StringLength.NameMaxLength} ký tự!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (Email.Length > Constants.StringLength.EmailMaxLength)
            {
                TempData["Error"] = $"Email không được quá {Constants.StringLength.EmailMaxLength} ký tự!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            // ✅ VALIDATION EMAIL FORMAT
            if (!EmailRegex().IsMatch(Email))
            {
                TempData["Error"] = "Email không hợp lệ!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            // ✅ VALIDATION PASSWORD STRENGTH
            if (Password.Length < Constants.Auth.PasswordMinLength)
            {
                TempData["Error"] = $"Mật khẩu phải có ít nhất {Constants.Auth.PasswordMinLength} ký tự!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (!UppercaseRegex().IsMatch(Password))
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 1 chữ hoa!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (!SpecialCharRegex().IsMatch(Password))
            {
                TempData["Error"] = "Mật khẩu phải có ít nhất 1 ký tự đặc biệt (@$!%*?&#)!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            // ✅ VALIDATION PHONE NUMBER
            if (!PhoneRegex().IsMatch(PhoneNumber))
            {
                TempData["Error"] = "Số điện thoại không hợp lệ! (VD: 0901234567)";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (Password != ConfirmPassword)
            {
                TempData["Error"] = "Mật khẩu và xác nhận mật khẩu không khớp!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            Email = Email.Trim().ToLower();
            PhoneNumber = PhoneNumber.Trim();
            FullName = FullName.Trim();

            if (await _context.Users.AnyAsync(u => u.Email == Email))
            {
                TempData["Error"] = "Email đã được sử dụng!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }

            if (await _context.Users.AnyAsync(u => u.PhoneNumber == PhoneNumber))
            {
                TempData["Error"] = "Số điện thoại đã được sử dụng!";
                ViewBag.ReturnUrl = ReturnUrl;
                return View();
            }


            var newUser = new User
            {
                FullName = FullName.Trim(),
                Email = Email,
                PasswordHash = HashPassword(Password),
                PhoneNumber = PhoneNumber.Trim(),
                Gender = Gender,
                CreatedAt = DateTime.Now,
                IsAdmin = false,
                IsActive = true
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Tạo tài khoản thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login", new { ReturnUrl });
        }

        // ĐĂNG XUẤT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Xóa TempData trước khi clear session
            TempData.Clear();

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();

            // ✅ Thêm headers để ngăn cache (using proper properties)
            Response.GetTypedHeaders().CacheControl = new Microsoft.Net.Http.Headers.CacheControlHeaderValue
            {
                NoCache = true,
                NoStore = true,
                MustRevalidate = true
            };
            Response.Headers.Pragma = "no-cache";
            Response.Headers.Expires = "0";

            return RedirectToAction("Index", "Home");
        }

        // [GET] /Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            return user == null ? RedirectToAction("Login") : View(user);
        }

        // [POST] /Account/UpdateProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FullName, string PhoneNumber, string? Gender)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng!";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                return RedirectToAction("Profile");
            }

            // Kiểm tra số điện thoại đã được sử dụng bởi người khác chưa
            var phoneExists = await _context.Users.AnyAsync(u => u.PhoneNumber == PhoneNumber.Trim() && u.Id != userId);
            if (phoneExists)
            {
                TempData["Error"] = "Số điện thoại đã được sử dụng bởi tài khoản khác!";
                return RedirectToAction("Profile");
            }

            user.FullName = FullName.Trim();
            user.PhoneNumber = PhoneNumber.Trim();
            user.Gender = Gender;

            await _context.SaveChangesAsync();

            // Cập nhật session
            HttpContext.Session.SetString(Constants.SessionKeys.UserName, user.FullName);

            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        // [POST] /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string NewPassword, string ConfirmNewPassword)
        {
            if (!User.Identity?.IsAuthenticated ?? true)
                return RedirectToAction("Login");

            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin người dùng!";
                return RedirectToAction("Profile");
            }

            if (string.IsNullOrWhiteSpace(CurrentPassword) || string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmNewPassword))
            {
                TempData["Error"] = "Vui lòng điền đầy đủ thông tin!";
                return RedirectToAction("Profile");
            }

            if (!VerifyPassword(CurrentPassword, user.PasswordHash, out _))
            {
                TempData["Error"] = "Mật khẩu hiện tại không đúng!";
                return RedirectToAction("Profile");
            }

            if (NewPassword != ConfirmNewPassword)
            {
                TempData["Error"] = "Mật khẩu mới và xác nhận mật khẩu không khớp!";
                return RedirectToAction("Profile");
            }

            if (!IsStrongPassword(NewPassword))
            {
                TempData["Error"] = $"Mật khẩu mới phải có ít nhất {Constants.Auth.PasswordMinLength} ký tự, 1 chữ hoa và 1 ký tự đặc biệt (@$!%*?&#)!";
                return RedirectToAction("Profile");
            }

            user.PasswordHash = HashPassword(NewPassword);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        // HÀM MÃ HÓA
        private static string HashPassword(string password)
            => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        private static bool VerifyPassword(string password, string hash, out bool isOldHash)
        {
            isOldHash = false;
            try
            {
                if (BCrypt.Net.BCrypt.Verify(password, hash))
                {
                    return true;
                }
            }
            catch
            {
                // Không phải định dạng BCrypt hợp lệ
            }

            // Fallback sang SHA-256 cũ không salt
            var oldHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));
            if (oldHash == hash)
            {
                isOldHash = true;
                return true;
            }

            return false;
        }

        // [GET] /Account/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // [POST] /Account/ForgotPassword - Gửi OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Vui lòng nhập email!";
                return View();
            }

            email = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                TempData["Error"] = "Email không tồn tại trong hệ thống!";
                return View();
            }

            // Tạo mã OTP 6 số
            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

            // Lưu OTP vào session (có thời hạn 5 phút)
            HttpContext.Session.SetString(Constants.SessionKeys.ResetOtp, otp);
            HttpContext.Session.SetString(Constants.SessionKeys.ResetEmail, email);
            HttpContext.Session.SetString(Constants.SessionKeys.OtpExpiry, DateTime.Now.AddMinutes(5).ToString());
            HttpContext.Session.Remove(Constants.SessionKeys.OtpAttempts); // Reset bộ đếm khi tạo OTP mới

            try
            {
                // Gửi OTP qua email
                await _emailService.SendOtpEmailAsync(email, otp);
                TempData["Success"] = "Mã OTP đã được gửi đến email của bạn!";
                return RedirectToAction("VerifyOTP");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Không thể gửi email: {ex.Message}";
                return View();
            }
        }

        // [GET] /Account/VerifyOTP
        [HttpGet]
        public IActionResult VerifyOTP()
        {
            var resetEmail = HttpContext.Session.GetString(Constants.SessionKeys.ResetEmail);
            if (string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;
            return View();
        }

        // [POST] /Account/VerifyOTP - Xác thực OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOTP(string otp)
        {
            var savedOTP = HttpContext.Session.GetString(Constants.SessionKeys.ResetOtp);
            var resetEmail = HttpContext.Session.GetString(Constants.SessionKeys.ResetEmail);
            var expiryString = HttpContext.Session.GetString(Constants.SessionKeys.OtpExpiry);

            if (string.IsNullOrEmpty(savedOTP) || string.IsNullOrEmpty(resetEmail))
            {
                TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại!";
                return RedirectToAction("ForgotPassword");
            }

            // Kiểm tra thời hạn OTP
            if (!string.IsNullOrEmpty(expiryString) && DateTime.TryParse(expiryString, out var expiry))
            {
                if (DateTime.Now > expiry)
                {
                    HttpContext.Session.Remove(Constants.SessionKeys.ResetOtp);
                    HttpContext.Session.Remove(Constants.SessionKeys.ResetEmail);
                    HttpContext.Session.Remove(Constants.SessionKeys.OtpExpiry);
                    HttpContext.Session.Remove(Constants.SessionKeys.OtpAttempts);
                    TempData["Error"] = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới!";
                    return RedirectToAction("ForgotPassword");
                }
            }

            // Kiểm tra số lần thử OTP (chống brute-force)
            var attempts = HttpContext.Session.GetInt32(Constants.SessionKeys.OtpAttempts) ?? 0;
            if (attempts >= 5)
            {
                HttpContext.Session.Remove(Constants.SessionKeys.ResetOtp);
                HttpContext.Session.Remove(Constants.SessionKeys.ResetEmail);
                HttpContext.Session.Remove(Constants.SessionKeys.OtpExpiry);
                HttpContext.Session.Remove(Constants.SessionKeys.OtpAttempts);
                TempData["Error"] = "Nhập sai quá 5 lần. Vui lòng yêu cầu OTP mới!";
                return RedirectToAction("ForgotPassword");
            }

            if (otp?.Trim() != savedOTP)
            {
                HttpContext.Session.SetInt32(Constants.SessionKeys.OtpAttempts, attempts + 1);
                var remaining = 4 - attempts;
                TempData["Error"] = $"Mã OTP không đúng! Còn {remaining} lần thử.";
                ViewBag.Email = resetEmail;
                return View();
            }

            // OTP đúng, xóa counter và chuyển đến trang đặt lại mật khẩu
            HttpContext.Session.Remove(Constants.SessionKeys.OtpAttempts);
            HttpContext.Session.SetString(Constants.SessionKeys.OtpVerified, "true");
            return RedirectToAction("ResetPassword");
        }

        // [GET] /Account/ResetPassword
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verified = HttpContext.Session.GetString(Constants.SessionKeys.OtpVerified);
            var resetEmail = HttpContext.Session.GetString(Constants.SessionKeys.ResetEmail);

            if (verified != "true" || string.IsNullOrEmpty(resetEmail))
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;
            return View();
        }

        // [POST] /Account/ResetPassword - Đặt lại mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string newPassword, string confirmPassword)
        {
            var verified = HttpContext.Session.GetString(Constants.SessionKeys.OtpVerified);
            var resetEmail = HttpContext.Session.GetString(Constants.SessionKeys.ResetEmail);

            if (verified != "true" || string.IsNullOrEmpty(resetEmail))
            {
                TempData["Error"] = "Phiên làm việc không hợp lệ!";
                return RedirectToAction("ForgotPassword");
            }

            if (string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ thông tin!";
                ViewBag.Email = resetEmail;
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["Error"] = "Mật khẩu và xác nhận mật khẩu không khớp!";
                ViewBag.Email = resetEmail;
                return View();
            }

            if (!IsStrongPassword(newPassword))
            {
                TempData["Error"] = $"Mật khẩu phải có ít nhất {Constants.Auth.PasswordMinLength} ký tự, 1 chữ hoa và 1 ký tự đặc biệt (@$!%*?&#)!";
                ViewBag.Email = resetEmail;
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == resetEmail);
            if (user == null)
            {
                TempData["Error"] = "Không tìm thấy người dùng!";
                return RedirectToAction("ForgotPassword");
            }

            user.PasswordHash = HashPassword(newPassword);
            await _context.SaveChangesAsync();

            // Xóa session
            HttpContext.Session.Remove(Constants.SessionKeys.ResetOtp);
            HttpContext.Session.Remove(Constants.SessionKeys.ResetEmail);
            HttpContext.Session.Remove(Constants.SessionKeys.OtpExpiry);
            HttpContext.Session.Remove(Constants.SessionKeys.OtpVerified);

            TempData["Success"] = "Đặt lại mật khẩu thành công! Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }
    }
}