namespace ClothingShop.Models.Requests
{
    // Class để deserialize selectedItems từ JSON
    public class SelectedCartItem
    {
        public string ProductId { get; set; } = "";
        public string Size { get; set; } = "";
        public string Color { get; set; } = "";
        public string Quantity { get; set; } = "";
    }
}
