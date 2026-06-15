using System.ComponentModel.DataAnnotations;

namespace ClothingShop.Models
{
    public class PaymentInfoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên ngân hàng")]
        [Display(Name = "Tên ngân hàng")]
        public string BankName { get; set; } = "Vietcombank";

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản")]
        [Display(Name = "Số tài khoản")]
        public string BankAccountNumber { get; set; } = "1234567890";

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản")]
        [Display(Name = "Tên chủ tài khoản")]
        public string BankAccountName { get; set; } = "NGUYEN VAN A";

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại MoMo")]
        [Display(Name = "Số điện thoại MoMo")]
        public string MoMoPhone { get; set; } = "0901234567";

        [Required(ErrorMessage = "Vui lòng nhập tên MoMo")]
        [Display(Name = "Tên MoMo")]
        public string MoMoName { get; set; } = "NGUYEN VAN A";
    }
}
