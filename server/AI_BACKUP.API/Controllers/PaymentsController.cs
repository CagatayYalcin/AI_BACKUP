using AI_BACKUP.Application.DTOs;
using AI_BACKUP.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly ISubscriptionService _subscriptionService;

        public PaymentsController(IPaymentService paymentService, ISubscriptionService subscriptionService)
        {
            _paymentService = paymentService;
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetUserPayments()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
            return Ok(payments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(int id)
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            // Check if user has access to this payment
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(payment.SubscriptionId);
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return Ok(payment);
        }

        [HttpPost("initiate")]
        public async Task<ActionResult<PaymentResponseDto>> InitiatePayment(PaymentRequestDto paymentRequestDto)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(paymentRequestDto.SubscriptionId);
            
            // Check if user has access to this subscription
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var response = await _paymentService.InitiatePaymentAsync(paymentRequestDto);
            return Ok(response);
        }

        [HttpPost("callback/{provider}")]
        [AllowAnonymous]
        public async Task<IActionResult> ProcessPaymentCallback(string provider, [FromForm] Dictionary<string, string> callbackParams)
        {
            var response = await _paymentService.ProcessPaymentCallbackAsync(provider, callbackParams);
            
            // Redirect to the appropriate page based on payment status
            if (response.Success)
            {
                return Redirect(response.RedirectUrl ?? "/payment/success");
            }
            else
            {
                return Redirect(response.RedirectUrl ?? "/payment/failed");
            }
        }

        // Admin endpoints
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<PaymentDto>>> GetAllPayments()
        {
            var payments = await _paymentService.GetAllPaymentsAsync();
            return Ok(payments);
        }

        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePayment(int id, UpdatePaymentDto updatePaymentDto)
        {
            await _paymentService.UpdatePaymentAsync(id, updatePaymentDto);
            return NoContent();
        }

        [HttpPost("admin/{id}/refund")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<PaymentResponseDto>> RefundPayment(int id, [FromBody] decimal amount)
        {
            var response = await _paymentService.RefundPaymentAsync(id, amount);
            return Ok(response);
        }
    }
}