using Microsoft.AspNetCore.Mvc;
using PaymentService.Services;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IAccountService _service;
        public PaymentsController(IAccountService service) => _service = service;

        [HttpPost("create")]
        public async Task<IActionResult> CreateAccount([FromQuery] Guid userId)
        {
            var acc = await _service.CreateAccountAsync(userId);
            return Ok(acc);
        }

        [HttpPost("topup")]
        public async Task<IActionResult> TopUp([FromQuery] Guid userId, [FromQuery] decimal amount)
        {
            await _service.TopUpAsync(userId, amount);
            return NoContent();
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance([FromQuery] Guid userId)
        {
            var balance = await _service.GetBalanceAsync(userId);
            return Ok(balance);
        }
    }
}