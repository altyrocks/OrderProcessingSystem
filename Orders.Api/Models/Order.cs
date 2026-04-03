namespace Orders.Api.Models;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}