using PaymentService.Models;

namespace PaymentService.Services
{
    public interface IAccountService
    {
        Task<Account> CreateAccountAsync(Guid userId);
        Task<decimal> GetBalanceAsync(Guid userId);
        Task TopUpAsync(Guid userId, decimal amount);
        Task HandleOrderCreatedAsync(Guid orderId, Guid userId, decimal amount, string messageId);
    }
}