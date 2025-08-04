using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface ISupportService
    {
        // Ticket operations
        Task<SupportTicketDto> GetTicketByIdAsync(int id);
        Task<SupportTicketDto> GetTicketByNumberAsync(string ticketNumber);
        Task<IReadOnlyList<SupportTicketDto>> GetAllTicketsAsync();
        Task<IReadOnlyList<SupportTicketDto>> GetTicketsByUserIdAsync(int userId);
        Task<IReadOnlyList<SupportTicketDto>> GetTicketsByAssignedUserIdAsync(int assignedUserId);
        Task<IReadOnlyList<SupportTicketDto>> GetTicketsByStatusAsync(string status);
        Task<SupportTicketDto> CreateTicketAsync(CreateSupportTicketDto createSupportTicketDto);
        Task UpdateTicketAsync(int id, UpdateSupportTicketDto updateSupportTicketDto);
        Task AssignTicketAsync(int id, int assignedToUserId);
        Task CloseTicketAsync(int id);
        Task ReopenTicketAsync(int id);
        
        // Ticket message operations
        Task<TicketMessageDto> GetTicketMessageByIdAsync(int id);
        Task<IReadOnlyList<TicketMessageDto>> GetMessagesByTicketIdAsync(int ticketId);
        Task<TicketMessageDto> AddTicketMessageAsync(CreateTicketMessageDto createTicketMessageDto);
        
        // Support operations
        Task<string> GenerateTicketNumberAsync();
        Task SendTicketNotificationAsync(int ticketId, string action);
    }
}