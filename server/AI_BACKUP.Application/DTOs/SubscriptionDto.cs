using AI_BACKUP.Core.Entities;
using System;

namespace AI_BACKUP.Application.DTOs
{
    public class SubscriptionPlanDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int MaxClients { get; set; }
        public double MaxStorageGb { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubscriptionPlanDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int MaxClients { get; set; }
        public double MaxStorageGb { get; set; }
        public bool IsActive { get; set; }
    }

    public class UpdateSubscriptionPlanDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
        public int MaxClients { get; set; }
        public double MaxStorageGb { get; set; }
        public bool IsActive { get; set; }
    }

    public class SubscriptionDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public SubscriptionStatus Status { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
        public SubscriptionPlanDto SubscriptionPlan { get; set; }
    }

    public class CreateSubscriptionDto
    {
        public int UserId { get; set; }
        public int SubscriptionPlanId { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public bool AutoRenew { get; set; }
    }

    public class UpdateSubscriptionDto
    {
        public SubscriptionStatus Status { get; set; }
        public BillingCycle BillingCycle { get; set; }
        public DateTime EndDate { get; set; }
        public bool AutoRenew { get; set; }
    }
}