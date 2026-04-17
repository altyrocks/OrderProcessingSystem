using Microsoft.Extensions.Caching.Distributed;
using Orders.Api.Services;

namespace Orders.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var redisConnectionString = builder.Configuration["Redis:ConnectionString"];

            if (string.IsNullOrWhiteSpace(redisConnectionString))
            {
                builder.Services.AddDistributedMemoryCache();
            }
            else
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnectionString;
                    options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "ordersystem:";
                });
            }

            builder.Services.AddSingleton<OrderStore>();
            builder.Services.AddSingleton<OrderReadCache>();
            builder.Services.AddSingleton<ProcessedEventStore>();
            builder.Services.AddSingleton<ServiceBusPublisher>();
            builder.Services.AddHostedService<OrderEventsSubscriber>();
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll",
                    policy => policy.AllowAnyOrigin()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod());
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("AllowAll");

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}