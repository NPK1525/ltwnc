namespace ClothingShop.Common
{
    /// <summary>
    /// Application-wide constants
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// Session timeout and authentication settings
        /// </summary>
        public static class Auth
        {
            public const int SessionTimeoutMinutes = 30;
            public const int PasswordMinLength = 6;
            public const int PasswordMaxLength = 100;
        }

        /// <summary>
        /// String length constraints
        /// </summary>
        public static class StringLength
        {
            public const int NameMaxLength = 100;
            public const int EmailMaxLength = 100;
            public const int PhoneMaxLength = 20;
            public const int DescriptionMaxLength = 1000;
            public const int AddressMaxLength = 500;
            public const int CommentMaxLength = 1000;
            public const int TitleMaxLength = 200;
        }

        /// <summary>
        /// Business logic constants
        /// </summary>
        public static class Business
        {
            public const decimal FreeShippingThreshold = 500000; // 500k VND
            public const decimal StandardShippingFee = 20000;    // 20k VND
            public const int OtpExpiryMinutes = 5;
            public const int OtpLength = 6;
        }

        /// <summary>
        /// Pagination settings
        /// </summary>
        public static class Pagination
        {
            public const int DefaultPageSize = 20;
            public const int AdminPageSize = 20;
            public const int ProductsPerPage = 20;
            public const int ReviewsPerPage = 10;
            public const int OrdersPerPage = 10;
        }

        /// <summary>
        /// VNPay payment constants
        /// </summary>
        public static class VNPay
        {
            public const int AmountMultiplier = 100; // VNPay requires amount * 100
        }

        /// <summary>
        /// Session keys
        /// </summary>
        public static class SessionKeys
        {
            public const string UserId = "UserId";
            public const string UserName = "UserName";
            public const string IsAdmin = "IsAdmin";
            public const string BuyNowProductId = "BuyNowProductId";
            public const string BuyNowQuantity = "BuyNowQuantity";
            public const string BuyNowSize = "BuyNowSize";
            public const string BuyNowColor = "BuyNowColor";
            public const string OtpCode = "OtpCode";
            public const string OtpExpiry = "OTPExpiry"; // matches "OTPExpiry" used in session
            public const string ResetEmail = "ResetEmail";
            public const string ResetOtp = "ResetOTP";
            public const string OtpAttempts = "OTPAttempts";
            public const string OtpVerified = "OTPVerified";
            public const string PendingOrderFullName = "PendingOrder_FullName";
            public const string PendingOrderPhoneNumber = "PendingOrder_PhoneNumber";
            public const string PendingOrderAddress = "PendingOrder_Address";
            public const string PendingOrderNote = "PendingOrder_Note";
            public const string PendingOrderPaymentMethod = "PendingOrder_PaymentMethod";
            public const string PendingOrderTotalAmount = "PendingOrder_TotalAmount";
            public const string PendingOrderVoucherCode = "PendingOrder_VoucherCode";
            public const string PendingOrderDiscountAmount = "PendingOrder_DiscountAmount";
            public const string PendingOrderTxnRef = "PendingOrder_TxnRef";
        }

        /// <summary>
        /// Default values
        /// </summary>
        public static class Defaults
        {
            public const int DefaultProductQuantity = 100;
            public const string DefaultProductColor = "Đen";
            public const string DefaultProductSize = "M";
            public const string DefaultGender = "Unisex";
        }
    }
}
