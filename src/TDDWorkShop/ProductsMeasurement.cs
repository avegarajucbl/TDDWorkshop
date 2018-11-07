namespace TDDWorkShop
{
    public class ProductsMeasurement
    {
        public ulong ProductId { get; set; }
        public string IdentificationCode { get; set; }
        public decimal Length { get; set; }
        public decimal Width { get; set; }
        public decimal Height { get; set; }
        public int MaxStackQuantity { get; set; }
        public int MaxClampQuantity { get; set; }
    }
}
