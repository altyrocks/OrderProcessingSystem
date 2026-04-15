using Orders.Api.Models;
using System.Collections.Concurrent;

namespace Orders.Api.Services;

public class OrderStore
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Order Add(Order order)
    {
        _orders[order.Id] = order;

        return order;
    }

    public IReadOnlyCollection<Order> GetAll()
    {
        return _orders.Values
            .OrderByDescending(order => order.CreatedAt)
            .ToArray();
    }

    public Order? Get(Guid id)
    {
        _orders.TryGetValue(id, out var order);

        return order;
    }

    public bool UpdateStatus(Guid id, string status)
    {
        if (!_orders.TryGetValue(id, out var order))
        {
            return false;
        }

        order.Status = status;

        return true;
    }
}