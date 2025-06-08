using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Outbox;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace OrderService.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _context;
        private readonly IConnection _connection;

        public OrderService(AppDbContext context, IConnection connection)
        {
            _context = context;
            _connection = connection;
        }

        public async Task<Order> CreateOrderAsync(Guid userId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var order = new Order { Id = Guid.NewGuid(), UserId = userId, Amount = amount };
            _context.Orders.Add(order);

            var msg = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(new { order.Id, order.UserId, order.Amount }),
                OccurredOn = DateTime.UtcNow,
                Published = false
            };
            _context.OutboxMessages.Add(msg);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            using var channel = _connection.CreateModel();
            channel.QueueDeclare("order.created", true, false, false);
            var body = Encoding.UTF8.GetBytes(msg.Payload);
            channel.BasicPublish("", "order.created", null, body);

            msg.Published = true;
            await _context.SaveChangesAsync();

            return order;
        }

        public async Task<IEnumerable<Order>> GetOrdersAsync(Guid userId)
            => await _context.Orders.Where(o => o.UserId == userId).ToListAsync();

        public async Task<Order?> GetOrderByIdAsync(Guid id)
            => await _context.Orders.FindAsync(id);
    }
}