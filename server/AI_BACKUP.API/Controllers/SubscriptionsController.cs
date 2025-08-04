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
    public class SubscriptionsController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionsController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetSubscriptionPlans()
        {
            var plans = await _subscriptionService.GetActiveSubscriptionPlansAsync();
            return Ok(plans);
        }

        [HttpGet("plans/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<SubscriptionPlanDto>> GetSubscriptionPlan(int id)
        {
            var plan = await _subscriptionService.GetSubscriptionPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound();
            }
            return Ok(plan);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetUserSubscriptions()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var subscriptions = await _subscriptionService.GetSubscriptionsByUserIdAsync(userId);
            return Ok(subscriptions);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SubscriptionDto>> GetSubscription(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // Check if user has access to this subscription
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return Ok(subscription);
        }

        [HttpPost]
        public async Task<ActionResult<SubscriptionDto>> CreateSubscription(CreateSubscriptionDto createSubscriptionDto)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            createSubscriptionDto.UserId = userId;

            var subscription = await _subscriptionService.CreateSubscriptionAsync(createSubscriptionDto);
            return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscription(int id, UpdateSubscriptionDto updateSubscriptionDto)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // Check if user has access to this subscription
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _subscriptionService.UpdateSubscriptionAsync(id, updateSubscriptionDto);
            return NoContent();
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelSubscription(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // Check if user has access to this subscription
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _subscriptionService.CancelSubscriptionAsync(id);
            return NoContent();
        }

        [HttpPost("{id}/renew")]
        public async Task<IActionResult> RenewSubscription(int id)
        {
            var subscription = await _subscriptionService.GetSubscriptionByIdAsync(id);
            if (subscription == null)
            {
                return NotFound();
            }

            // Check if user has access to this subscription
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (subscription.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            await _subscriptionService.RenewSubscriptionAsync(id);
            return NoContent();
        }

        // Admin endpoints
        [HttpGet("admin/plans")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetAllSubscriptionPlans()
        {
            var plans = await _subscriptionService.GetAllSubscriptionPlansAsync();
            return Ok(plans);
        }

        [HttpPost("admin/plans")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<SubscriptionPlanDto>> CreateSubscriptionPlan(CreateSubscriptionPlanDto createSubscriptionPlanDto)
        {
            var plan = await _subscriptionService.CreateSubscriptionPlanAsync(createSubscriptionPlanDto);
            return CreatedAtAction(nameof(GetSubscriptionPlan), new { id = plan.Id }, plan);
        }

        [HttpPut("admin/plans/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSubscriptionPlan(int id, UpdateSubscriptionPlanDto updateSubscriptionPlanDto)
        {
            await _subscriptionService.UpdateSubscriptionPlanAsync(id, updateSubscriptionPlanDto);
            return NoContent();
        }

        [HttpDelete("admin/plans/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteSubscriptionPlan(int id)
        {
            await _subscriptionService.DeleteSubscriptionPlanAsync(id);
            return NoContent();
        }

        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<SubscriptionDto>>> GetAllSubscriptions()
        {
            var subscriptions = await _subscriptionService.GetAllSubscriptionsAsync();
            return Ok(subscriptions);
        }
    }
}