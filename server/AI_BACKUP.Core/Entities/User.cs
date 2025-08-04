using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public UserRole Role { get; set; }
        public bool IsActive { get; set; }
        public string Company { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }
        public string Phone { get; set; }
        public string TaxId { get; set; } // VAT number or tax identification
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<Client> Clients { get; set; }
        public ICollection<BackupJob> BackupJobs { get; set; }
        public ICollection<StorageConfig> StorageConfigs { get; set; }
        public License License { get; set; }
        public ICollection<Subscription> Subscriptions { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
        public ICollection<SupportTicket> SupportTickets { get; set; }
        public ICollection<SupportTicket> AssignedTickets { get; set; }
    }

    public enum UserRole
    {
        Admin,
        User,
        Support,
        Billing
    }
}