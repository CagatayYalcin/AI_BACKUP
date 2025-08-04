using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentDto> GetPaymentByIdAsync(int id);
        Task<IReadOnlyList<PaymentDto>> GetAllPaymentsAsync();
        Task<IReadOnlyList<PaymentDto>> GetPaymentsByUserIdAsync(int userId);
        Task<IReadOnlyList<PaymentDto>> GetPaymentsBySubscriptionIdAsync(int subscriptionId);
        Task<IReadOnlyList<PaymentDto>> GetPaymentsByInvoiceIdAsync(int invoiceId);
        Task<PaymentDto> CreatePaymentAsync(CreatePaymentDto createPaymentDto);
        Task UpdatePaymentAsync(int id, UpdatePaymentDto updatePaymentDto);
        
        // Payment processing
        Task<PaymentResponseDto> InitiatePaymentAsync(PaymentRequestDto paymentRequestDto);
        Task<PaymentResponseDto> ProcessPaymentCallbackAsync(string provider, Dictionary<string, string> callbackParams);
        Task<PaymentResponseDto> CapturePaymentAsync(string transactionId);
        Task<PaymentResponseDto> RefundPaymentAsync(int paymentId, decimal amount);
    }
}