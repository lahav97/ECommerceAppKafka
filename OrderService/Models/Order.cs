namespace OrderService.Models
{
    public class Order
    {
        public string? OrderId { get; set; }
        public string? CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem>? Items { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Currency { get; set; }
        public string? Status { get; set; }
    }
}
