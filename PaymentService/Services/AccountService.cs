using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Inbox;
using PaymentService.Outbox;
using RabbitMQ.Client;
using System.Text.Json;
using System.Text;

namespace PaymentService.Services
{
    public class AccountService : IAccountService
    {
        private readonly AppDbContext _context;
        private readonly IConnection _connection;

        public AccountService(AppDbContext context, IConnection connection)
        {
            _context = context;
            _connection = connection;
        }

        public async Task<Account> CreateAccountAsync(Guid userId)
        {
            var account = new Account { Id = Guid.NewGuid(), UserId = userId, Balance = 0 };
            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();
            return account;
        }

        public async Task<decimal> GetBalanceAsync(Guid userId)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
            return account?.Balance ?? 0;
        }

        public async Task TopUpAsync(Guid userId, decimal amount)
        {
            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId)
                          ?? await CreateAccountAsync(userId);
            account.Balance += amount;
            await _context.SaveChangesAsync();
        }

        public async Task HandleOrderCreatedAsync(Guid orderId, Guid userId, decimal amount, string messageId)
        {
            // idempotency check
            if (await _context.InboxMessages.AnyAsync(i => i.MessageId == messageId))
                return;

            using var tx = await _context.Database.BeginTransactionAsync();
            _context.InboxMessages.Add(new InboxMessage { MessageId = messageId });

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.UserId == userId)
                          ?? await CreateAccountAsync(userId);
            if (account.Balance >= amount)
                account.Balance -= amount;

            var outMsg = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Payload = JsonSerializer.Serialize(new { orderId, success = account.Balance >= 0 }),
                OccurredOn = DateTime.UtcNow,
                Published = false
            };
            _context.OutboxMessages.Add(outMsg);

            await _context.SaveChangesAsync();
            await tx.CommitAsync();

            using var channel = _connection.CreateModel();
            channel.QueueDeclare("order.processed", true, false, false);
            var body = Encoding.UTF8.GetBytes(outMsg.Payload);
            channel.BasicPublish("", "order.processed", null, body);

            outMsg.Published = true;
            await _context.SaveChangesAsync();
        }
    }
}