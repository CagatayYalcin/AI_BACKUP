using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public enum SubscriptionStatus
    {
        Active,
        Expired,
        Cancelled,
        PendingPayment
    }

    public enum BillingCycle
    {
        Monthly,
        Yearly
    }

    public class Subscription
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public SubscriptionStatus Status { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }
        public ICollection<Payment> Payments { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
    }
}