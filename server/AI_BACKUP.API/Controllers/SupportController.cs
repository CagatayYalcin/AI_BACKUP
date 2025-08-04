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
    public class SupportController : ControllerBase
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService supportService)
        {
            _supportService = supportService;
        }

        [HttpGet("tickets")]
        public async Task<ActionResult<IEnumerable<SupportTicketDto>>> GetUserTickets()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var tickets = await _supportService.GetTicketsByUserIdAsync(userId);
            return Ok(tickets);
        }

        [HttpGet("tickets/{id}")]
        public async Task<ActionResult<SupportTicketDto>> GetTicket(int id)
        {
            var ticket = await _supportService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            // Check if user has access to this ticket
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (ticket.UserId != userId && ticket.AssignedToUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Support"))
            {
                return Forbid();
            }

            return Ok(ticket);
        }

        [HttpPost("tickets")]
        public async Task<ActionResult<SupportTicketDto>> CreateTicket(CreateSupportTicketDto createSupportTicketDto)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            createSupportTicketDto.UserId = userId;

            var ticket = await _supportService.CreateTicketAsync(createSupportTicketDto);
            return CreatedAtAction(nameof(GetTicket), new { id = ticket.Id }, ticket);
        }

        [HttpGet("tickets/{ticketId}/messages")]
        public async Task<ActionResult<IEnumerable<TicketMessageDto>>> GetTicketMessages(int ticketId)
        {
            var ticket = await _supportService.GetTicketByIdAsync(ticketId);
            if (ticket == null)
            {
                return NotFound();
            }

            // Check if user has access to this ticket
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (ticket.UserId != userId && ticket.AssignedToUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Support"))
            {
                return Forbid();
            }

            var messages = await _supportService.GetMessagesByTicketIdAsync(ticketId);
            return Ok(messages);
        }

        [HttpPost("tickets/{ticketId}/messages")]
        public async Task<ActionResult<TicketMessageDto>> AddTicketMessage(int ticketId, [FromBody] string message)
        {
            var ticket = await _supportService.GetTicketByIdAsync(ticketId);
            if (ticket == null)
            {
                return NotFound();
            }

            // Check if user has access to this ticket
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (ticket.UserId != userId && ticket.AssignedToUserId != userId && !User.IsInRole("Admin") && !User.IsInRole("Support"))
            {
                return Forbid();
            }

            var createMessageDto = new CreateTicketMessageDto
            {
                TicketId = ticketId,
                UserId = userId,
                Message = message,
                IsInternal = User.IsInRole("Admin") || User.IsInRole("Support")
            };

            var ticketMessage = await _supportService.AddTicketMessageAsync(createMessageDto);
            return Ok(ticketMessage);
        }

        // Support staff endpoints
        [HttpGet("staff/tickets")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<IEnumerable<SupportTicketDto>>> GetAssignedTickets()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var tickets = await _supportService.GetTicketsByAssignedUserIdAsync(userId);
            return Ok(tickets);
        }

        [HttpGet("staff/tickets/all")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<IEnumerable<SupportTicketDto>>> GetAllTickets()
        {
            var tickets = await _supportService.GetAllTicketsAsync();
            return Ok(tickets);
        }

        [HttpGet("staff/tickets/status/{status}")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<ActionResult<IEnumerable<SupportTicketDto>>> GetTicketsByStatus(string status)
        {
            var tickets = await _supportService.GetTicketsByStatusAsync(status);
            return Ok(tickets);
        }

        [HttpPut("staff/tickets/{id}")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> UpdateTicket(int id, UpdateSupportTicketDto updateSupportTicketDto)
        {
            await _supportService.UpdateTicketAsync(id, updateSupportTicketDto);
            return NoContent();
        }

        [HttpPost("staff/tickets/{id}/assign")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> AssignTicket(int id, [FromBody] int assignedToUserId)
        {
            await _supportService.AssignTicketAsync(id, assignedToUserId);
            return NoContent();
        }

        [HttpPost("staff/tickets/{id}/close")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> CloseTicket(int id)
        {
            await _supportService.CloseTicketAsync(id);
            return NoContent();
        }

        [HttpPost("staff/tickets/{id}/reopen")]
        [Authorize(Roles = "Admin,Support")]
        public async Task<IActionResult> ReopenTicket(int id)
        {
            await _supportService.ReopenTicketAsync(id);
            return NoContent();
        }
    }
}