using Microsoft.EntityFrameworkCore;
using PaymentService.Inbox;
using PaymentService.Outbox;
using PaymentService.Models;

namespace PaymentService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<InboxMessage>().HasKey(i => i.MessageId);
            modelBuilder.Entity<OutboxMessage>().HasKey(o => o.Id);
        }
    }
}