using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;

namespace Payment.Function.Services;

public class ServiceBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(IConfiguration config)
    {
        var connectionString = config["ServiceBusConnection"];

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender("orders");
    }

    public async Task PublishAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);
        var sbMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(sbMessage);
    }
}