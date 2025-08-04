using System;
using System.ComponentModel.DataAnnotations;

namespace AI_BACKUP.Application.DTOs
{
    public class TwoFactorVerificationDto
    {
        [Required]
        public Guid UserId { get; set; }
        
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }
    }

    public class EnableTwoFactorDto
    {
        [Required]
        public int Type { get; set; }
    }

    public class ExternalLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        
        public string FirstName { get; set; }
        
        public string LastName { get; set; }
        
        [Required]
        public string Token { get; set; }
        
        [Required]
        public string Provider { get; set; }
    }
}