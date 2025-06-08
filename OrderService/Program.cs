// OrderService/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OrderService.Data;
using OrderService.Services;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// 1. EF + подавление warning транзакций In-Memory
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("OrdersDb")
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
);

// 2. Сервис заказов
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

// 3. Подключение к RabbitMQ с retry-логикой
builder.Services.AddSingleton<IConnection>(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var factory = new ConnectionFactory { HostName = cfg["RabbitMQ:Host"] ?? "rabbitmq" };
    IConnection conn = null!;
    const int maxAttempts = 10;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            conn = factory.CreateConnection();
            Console.WriteLine("[x] order-service: RabbitMQ connected");
            break;
        }
        catch (BrokerUnreachableException)
        {
            Console.WriteLine($"[!] order-service: RabbitMQ not ready (attempt {attempt}/{maxAttempts}), sleeping 5s...");
            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
    }
    if (conn == null)
        throw new InvalidOperationException("Could not connect to RabbitMQ after multiple attempts");
    return conn;
});

// 4. MVC/Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();