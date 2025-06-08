using OrderService.Models;

namespace OrderService.Services
{
    public interface IOrderService
    {
        Task<Order> CreateOrderAsync(Guid userId, decimal amount);
        Task<IEnumerable<Order>> GetOrdersAsync(Guid userId);
        Task<Order?> GetOrderByIdAsync(Guid id);
    }
}