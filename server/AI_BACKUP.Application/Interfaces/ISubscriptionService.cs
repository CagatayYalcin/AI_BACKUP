using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface ISubscriptionService
    {
        // Subscription Plan operations
        Task<SubscriptionPlanDto> GetSubscriptionPlanByIdAsync(int id);
        Task<IReadOnlyList<SubscriptionPlanDto>> GetAllSubscriptionPlansAsync();
        Task<IReadOnlyList<SubscriptionPlanDto>> GetActiveSubscriptionPlansAsync();
        Task<SubscriptionPlanDto> CreateSubscriptionPlanAsync(CreateSubscriptionPlanDto createSubscriptionPlanDto);
        Task UpdateSubscriptionPlanAsync(int id, UpdateSubscriptionPlanDto updateSubscriptionPlanDto);
        Task DeleteSubscriptionPlanAsync(int id);

        // Subscription operations
        Task<SubscriptionDto> GetSubscriptionByIdAsync(int id);
        Task<IReadOnlyList<SubscriptionDto>> GetAllSubscriptionsAsync();
        Task<IReadOnlyList<SubscriptionDto>> GetSubscriptionsByUserIdAsync(int userId);
        Task<SubscriptionDto> GetActiveSubscriptionByUserIdAsync(int userId);
        Task<SubscriptionDto> CreateSubscriptionAsync(CreateSubscriptionDto createSubscriptionDto);
        Task UpdateSubscriptionAsync(int id, UpdateSubscriptionDto updateSubscriptionDto);
        Task CancelSubscriptionAsync(int id);
        Task RenewSubscriptionAsync(int id);
        
        // Subscription management
        Task<bool> CheckUserSubscriptionLimitsAsync(int userId, int clientCount, double storageUsageGb);
        Task ProcessSubscriptionRenewalsAsync();
        Task SendSubscriptionExpirationNotificationsAsync();
    }
}