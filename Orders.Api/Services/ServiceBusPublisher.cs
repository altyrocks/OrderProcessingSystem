using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace Orders.Api.Services;

public class ServiceBusPublisher
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public ServiceBusPublisher(IConfiguration config)
    {
        var connectionString = config["ServiceBus:ConnectionString"];
        var topicName = config["ServiceBus:TopicName"];

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(topicName);
    }

    public async Task PublishAsync<T>(T message)
    {
        var json = JsonSerializer.Serialize(message);

        var serviceBusMessage = new ServiceBusMessage(json)
        {
            ContentType = "application/json"
        };

        await _sender.SendMessageAsync(serviceBusMessage);
    }
}