using AI_BACKUP.Core.Entities;
using System;
using System.Collections.Generic;

namespace AI_BACKUP.Application.DTOs
{
    public class InvoiceDto
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
        public UserDto User { get; set; }
        public IEnumerable<PaymentDto> Payments { get; set; }
    }

    public class CreateInvoiceDto
    {
        public int SubscriptionId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime DueDate { get; set; }
        public string BillingAddress { get; set; }
        public string Notes { get; set; }
    }

    public class UpdateInvoiceDto
    {
        public InvoiceStatus Status { get; set; }
        public decimal Amount { get; set; }
        public decimal TaxAmount { get; set; }
        public DateTime DueDate { get; set; }
        public string BillingAddress { get; set; }
        public string Notes { get; set; }
    }
}