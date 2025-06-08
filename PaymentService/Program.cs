// PaymentService/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PaymentService.Data;
using PaymentService.Services;
using PaymentService.BackgroundServices;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;

var builder2 = WebApplication.CreateBuilder(args);

// 1. EF + подавление warning транзакций In-Memory
builder2.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("PaymentsDb")
       .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
);

// 2. Сервис аккаунтов
builder2.Services.AddScoped<IAccountService, AccountService>();

// 3. Подключение к RabbitMQ с retry-логикой
builder2.Services.AddSingleton<IConnection>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var host = cfg["RabbitMQ:Host"] ?? "rabbitmq";
    var factory = new ConnectionFactory
    {
        HostName = host,
        AutomaticRecoveryEnabled = true,
        DispatchConsumersAsync = true
    };
    IConnection connection = null!;
    const int maxAttempts = 10;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            connection = factory.CreateConnection();
            Console.WriteLine($"[x] payment-service: RabbitMQ connected to {host}");
            break;
        }
        catch (BrokerUnreachableException)
        {
            Console.WriteLine($"[!] payment-service: RabbitMQ not ready (attempt {attempt}/{maxAttempts}), sleeping 5s...");
            Thread.Sleep(5000);
        }
    }
    if (connection == null)
        throw new InvalidOperationException($"RabbitMQ unreachable at {host} after {maxAttempts} attempts.");
    return connection;
});

// 4. Background consumer для заказов
builder2.Services.AddHostedService<OrderConsumerService>();

// 5. MVC/Swagger
builder2.Services.AddControllers();
builder2.Services.AddEndpointsApiExplorer();
builder2.Services.AddSwaggerGen();

var app2 = builder2.Build();
app2.UseSwagger();
app2.UseSwaggerUI();
app2.MapControllers();
app2.Run();