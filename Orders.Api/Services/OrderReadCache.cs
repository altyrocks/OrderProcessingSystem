using System.Text.Json;
using Orders.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace Orders.Api.Services;

public class OrderReadCache
{
    private const string OrdersListKey = "orders:all";
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly IDistributedCache _cache;

    public OrderReadCache(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<IReadOnlyCollection<Order>?> GetAllAsync()
    {
        var json = await _cache.GetStringAsync(OrdersListKey);

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<List<Order>>(json);
    }

    public async Task<Order?> GetAsync(Guid id)
    {
        var json = await _cache.GetStringAsync(GetOrderKey(id));

        return string.IsNullOrWhiteSpace(json)
            ? null
            : JsonSerializer.Deserialize<Order>(json);
    }

    public Task SetAllAsync(IReadOnlyCollection<Order> orders)
    {
        return _cache.SetStringAsync(OrdersListKey, JsonSerializer.Serialize(orders), CacheOptions);
    }

    public Task SetAsync(Order order)
    {
        return _cache.SetStringAsync(GetOrderKey(order.Id), JsonSerializer.Serialize(order), CacheOptions);
    }

    public async Task RemoveAsync(Guid id)
    {
        await _cache.RemoveAsync(GetOrderKey(id));
        await _cache.RemoveAsync(OrdersListKey);
    }

    public async Task RefreshAsync(Order order, IReadOnlyCollection<Order> allOrders)
    {
        await SetAsync(order);
        await SetAllAsync(allOrders);
    }

    private static string GetOrderKey(Guid id) => $"orders:{id}";
}