using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using PaymentService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentService.BackgroundServices
{
    public class OrderConsumerService : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IAccountService _accountService;

        public OrderConsumerService(IConnection connection, IAccountService accountService)
        {
            _connection = connection;
            _accountService = accountService;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare("order.created", durable: true, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var payload = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var meta = JsonSerializer.Deserialize<JsonElement>(payload);
                    var orderId = meta.GetProperty("Id").GetGuid();
                    var userId  = meta.GetProperty("UserId").GetGuid();
                    var amount  = meta.GetProperty("Amount").GetDecimal();
                    var msgId   = ea.BasicProperties.MessageId ?? Guid.NewGuid().ToString();

                    await _accountService.HandleOrderCreatedAsync(orderId, userId, amount, msgId);

                    // если нужно ручное подтверждение:
                    // channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    // логгирование ошибки или обработка Nack
                    Console.Error.WriteLine($"[OrderConsumerService] Error processing message: {ex}");
                }
            };

            channel.BasicConsume(
                queue: "order.created",
                autoAck: true,     // или false, если вы хотите вручную ack
                consumer: consumer
            );

            return Task.CompletedTask;
        }
    }
}
