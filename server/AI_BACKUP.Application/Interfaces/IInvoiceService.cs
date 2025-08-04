using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> GetInvoiceByIdAsync(int id);
        Task<InvoiceDto> GetInvoiceByNumberAsync(string invoiceNumber);
        Task<IReadOnlyList<InvoiceDto>> GetAllInvoicesAsync();
        Task<IReadOnlyList<InvoiceDto>> GetInvoicesByUserIdAsync(int userId);
        Task<IReadOnlyList<InvoiceDto>> GetInvoicesBySubscriptionIdAsync(int subscriptionId);
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto);
        Task UpdateInvoiceAsync(int id, UpdateInvoiceDto updateInvoiceDto);
        Task DeleteInvoiceAsync(int id);
        
        // Invoice operations
        Task<string> GenerateInvoiceNumberAsync();
        Task<Stream> GenerateInvoicePdfAsync(int invoiceId);
        Task SendInvoiceEmailAsync(int invoiceId);
        Task MarkInvoiceAsPaidAsync(int invoiceId);
        Task MarkInvoiceAsOverdueAsync(int invoiceId);
        Task ProcessOverdueInvoicesAsync();
    }
}