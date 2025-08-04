using System;
using System.ComponentModel.DataAnnotations;

namespace AI_BACKUP.API.Models
{
    public class TwoFactorAuthModel
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        public bool IsEnabled { get; set; }
        
        [Required]
        public TwoFactorType Type { get; set; }
        
        public string? SecretKey { get; set; }
        
        public string? PhoneNumber { get; set; }
        
        public string? Email { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public DateTime? LastUsedAt { get; set; }
        
        public virtual User User { get; set; }
    }

    public enum TwoFactorType
    {
        Authenticator = 0,
        SMS = 1,
        Email = 2
    }
}