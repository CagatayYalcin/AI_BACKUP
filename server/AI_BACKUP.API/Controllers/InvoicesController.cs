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
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetUserInvoices()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            var invoices = await _invoiceService.GetInvoicesByUserIdAsync(userId);
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // Check if user has access to this invoice
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (invoice.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            return Ok(invoice);
        }

        [HttpGet("{id}/pdf")]
        public async Task<IActionResult> GetInvoicePdf(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            // Check if user has access to this invoice
            var userId = int.Parse(User.FindFirst("UserId")?.Value);
            if (invoice.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var pdfStream = await _invoiceService.GenerateInvoicePdfAsync(id);
            return File(pdfStream, "application/pdf", $"invoice-{invoice.InvoiceNumber}.pdf");
        }

        // Admin endpoints
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetAllInvoices()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        [HttpPost("admin")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InvoiceDto>> CreateInvoice(CreateInvoiceDto createInvoiceDto)
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(createInvoiceDto);
            return CreatedAtAction(nameof(GetInvoice), new { id = invoice.Id }, invoice);
        }

        [HttpPut("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateInvoice(int id, UpdateInvoiceDto updateInvoiceDto)
        {
            await _invoiceService.UpdateInvoiceAsync(id, updateInvoiceDto);
            return NoContent();
        }

        [HttpDelete("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            await _invoiceService.DeleteInvoiceAsync(id);
            return NoContent();
        }

        [HttpPost("admin/{id}/send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendInvoice(int id)
        {
            await _invoiceService.SendInvoiceEmailAsync(id);
            return NoContent();
        }

        [HttpPost("admin/{id}/mark-paid")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkInvoiceAsPaid(int id)
        {
            await _invoiceService.MarkInvoiceAsPaidAsync(id);
            return NoContent();
        }

        [HttpPost("admin/{id}/mark-overdue")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkInvoiceAsOverdue(int id)
        {
            await _invoiceService.MarkInvoiceAsOverdueAsync(id);
            return NoContent();
        }
    }
}