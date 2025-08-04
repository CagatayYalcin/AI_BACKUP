using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public enum InvoiceStatus
    {
        Draft,
        Sent,
        Paid,
        Overdue,
        Cancelled
    }

    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public string BillingAddress { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public Subscription Subscription { get; set; }
        public User User { get; set; }
        public ICollection<Payment> Payments { get; set; }
    }
}