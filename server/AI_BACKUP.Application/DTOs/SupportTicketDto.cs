using AI_BACKUP.Core.Entities;
using System;
using System.Collections.Generic;

namespace AI_BACKUP.Application.DTOs
{
    public class SupportTicketDto
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
        public UserDto User { get; set; }
        public UserDto AssignedToUser { get; set; }
        public IEnumerable<TicketMessageDto> Messages { get; set; }
    }

    public class CreateSupportTicketDto
    {
        public int UserId { get; set; }
        public string Subject { get; set; }
        public string Description { get; set; }
        public TicketPriority Priority { get; set; }
    }

    public class UpdateSupportTicketDto
    {
        public TicketStatus Status { get; set; }
        public TicketPriority Priority { get; set; }
        public int? AssignedToUserId { get; set; }
    }

    public class TicketMessageDto
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsInternal { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto User { get; set; }
    }

    public class CreateTicketMessageDto
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Message { get; set; }
        public bool IsInternal { get; set; }
    }
}