using AI_BACKUP.Core.Entities;
using System;

namespace AI_BACKUP.Application.DTOs
{
    public class PaymentDto
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; }
        public string PaymentDetails { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        public int SubscriptionId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string PaymentDetails { get; set; }
    }

    public class UpdatePaymentDto
    {
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; }
    }

    public class PaymentRequestDto
    {
        public int SubscriptionId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string ReturnUrl { get; set; }
        public string CancelUrl { get; set; }
    }

    public class PaymentResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string RedirectUrl { get; set; }
        public string TransactionId { get; set; }
        public PaymentStatus Status { get; set; }
    }
}