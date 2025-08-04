using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public enum TicketStatus
    {
        Open,
        InProgress,
        Resolved,
        Closed
    }

    public enum TicketPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public class SupportTicket
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; }
        public int UserId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public int? AssignedToUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
        public User AssignedToUser { get; set; }
        public ICollection<TicketMessage> Messages { get; set; }
    }

    public class TicketMessage
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsInternal { get; set; } // For staff-only notes
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public SupportTicket Ticket { get; set; }
        public User User { get; set; }
    }
}