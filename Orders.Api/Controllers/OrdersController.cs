using Shared.Messaging;
using Orders.Api.Models;
using Orders.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ServiceBusPublisher publisher, OrderStore orderStore) : ControllerBase
{
    private readonly ServiceBusPublisher _publisher = publisher;
    private readonly OrderStore _orderStore = orderStore;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        order.Id = Guid.NewGuid();
        order.Status = "Pending";
        order.CreatedAt = DateTime.UtcNow;

        _orderStore.Add(order);

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
        return Ok(_orderStore.GetAll());
    }

    [HttpGet("{id}")]
    public IActionResult GetOrder(Guid id)
    {
        var order = _orderStore.Get(id);

        if (order == null)
            return NotFound();

        return Ok(new
        {
            orderId = order.Id,
            status = order.Status
        });
    }
}