namespace ClothingShop.Models.Requests
{
    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
    }

    public class RemoveItemRequest
    {
        public int ProductId { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
    }
}
