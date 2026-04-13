using Orders.Api.Models;
using Orders.Api.Services;
using Shared.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ServiceBusPublisher publisher) : ControllerBase
{
    private static readonly List<Order> _orders = new();

    private readonly ServiceBusPublisher _publisher = publisher;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        order.Id = Guid.NewGuid();
        order.Status = "Pending";
        order.CreatedAt = DateTime.UtcNow;

        _orders.Add(order);

        var orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            ProductName = order.ProductName,
            Quantity = order.Quantity
        };

        var correlationId = Guid.NewGuid().ToString();

        var envelope = new EventEnvelope<OrderCreatedEvent>
        {
            EventType = "OrderCreated",
            CorrelationId = correlationId,
            Data = orderCreatedEvent
        };

        await _publisher.PublishAsync(envelope);

        return Ok(order);
    }

    [HttpGet]
    public IActionResult GetOrders()
    {
        return Ok(_orders);
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(Guid id)
    {
        var order = _orders.FirstOrDefault(o => o.Id == id);

        if (order == null)
            return NotFound();

        return Ok(new
        {
            orderId = order.Id,
            status = order.Status
        });
    }
}