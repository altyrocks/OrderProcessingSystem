using Orders.Api.Models;
using Orders.Api.Services;
using Shared.Messaging;
using Microsoft.AspNetCore.Mvc;

namespace Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController(ServiceBusPublisher publisher, OrderStore orderStore, OrderReadCache orderReadCache) : ControllerBase
{
    private readonly ServiceBusPublisher _publisher = publisher;
    private readonly OrderStore _orderStore = orderStore;
    private readonly OrderReadCache _orderReadCache = orderReadCache;

    [HttpPost]
    public async Task<IActionResult> CreateOrder(Order order)
    {
        order.Id = Guid.NewGuid();
        order.Status = "Pending";
        order.CreatedAt = DateTime.UtcNow;

        _orderStore.Add(order);
        await _orderReadCache.RefreshAsync(order, _orderStore.GetAll());

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
    public async Task<IActionResult> GetOrders()
    {
        var cachedOrders = await _orderReadCache.GetAllAsync();

        if (cachedOrders != null)
        {
            return Ok(cachedOrders);
        }

        var orders = _orderStore.GetAll();
        await _orderReadCache.SetAllAsync(orders);

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var cachedOrder = await _orderReadCache.GetAsync(id);

        if (cachedOrder != null)
        {
            return Ok(new
            {
                orderId = cachedOrder.Id,
                status = cachedOrder.Status
            });
        }

        var order = _orderStore.Get(id);

        if (order == null)
            return NotFound();

        await _orderReadCache.SetAsync(order);

        return Ok(new
        {
            orderId = order.Id,
            status = order.Status
        });
    }
}
