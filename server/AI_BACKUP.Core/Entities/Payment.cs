using System;

namespace AI_BACKUP.Core.Entities
{
    public enum PaymentMethod
    {
        CreditCard,
        PayPal,
        BankTransfer,
        Other
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed,
        Refunded
    }

    public class Payment
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public int? InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus Status { get; set; }
        public string TransactionId { get; set; }
        public string PaymentDetails { get; set; } // JSON with payment provider details
        public DateTime PaymentDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Subscription Subscription { get; set; }
        public Invoice Invoice { get; set; }
    }
}