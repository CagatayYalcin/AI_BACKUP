using AI_BACKUP.API.Models;
using AI_BACKUP.API.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using OtpNet;

namespace AI_BACKUP.API.Services
{
    public class TwoFactorAuthService : ITwoFactorAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ISmsService _smsService;

        public TwoFactorAuthService(
            ApplicationDbContext context,
            IConfiguration configuration,
            IEmailService emailService,
            ISmsService smsService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
            _smsService = smsService;
        }

        public async Task<bool> EnableTwoFactorAuthAsync(Guid userId, TwoFactorType type)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new ArgumentException("User not found");
            }

            var existingAuth = await _context.TwoFactorAuth
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingAuth != null)
            {
                existingAuth.IsEnabled = true;
                existingAuth.Type = type;
                existingAuth.LastUsedAt = DateTime.UtcNow;
            }
            else
            {
                var twoFactorAuth = new TwoFactorAuthModel
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    IsEnabled = true,
                    Type = type,
                    CreatedAt = DateTime.UtcNow
                };

                if (type == TwoFactorType.Authenticator)
                {
                    // Generate a random secret key for authenticator app
                    var secretKey = GenerateSecretKey();
                    twoFactorAuth.SecretKey = secretKey;
                }
                else if (type == TwoFactorType.Email)
                {
                    twoFactorAuth.Email = user.Email;
                }
                else if (type == TwoFactorType.SMS)
                {
                    twoFactorAuth.PhoneNumber = user.PhoneNumber;
                }

                await _context.TwoFactorAuth.AddAsync(twoFactorAuth);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DisableTwoFactorAuthAsync(Guid userId)
        {
            var twoFactorAuth = await _context.TwoFactorAuth
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (twoFactorAuth == null)
            {
                return false;
            }

            twoFactorAuth.IsEnabled = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code)
        {
            var twoFactorAuth = await _context.TwoFactorAuth
                .FirstOrDefaultAsync(t => t.UserId == userId && t.IsEnabled);

            if (twoFactorAuth == null)
            {
                return false;
            }

            bool isValid = false;

            switch (twoFactorAuth.Type)
            {
                case TwoFactorType.Authenticator:
                    isValid = VerifyAuthenticatorCode(twoFactorAuth.SecretKey, code);
                    break;
                case TwoFactorType.SMS:
                case TwoFactorType.Email:
                    // For SMS and Email, we verify against the code stored in the session or cache
                    // This would be implemented with a separate service for managing verification codes
                    isValid = await VerifyCodeFromCache(userId, code);
                    break;
                default:
                    return false;
            }

            if (isValid)
            {
                twoFactorAuth.LastUsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return isValid;
        }

        public async Task<bool> SendTwoFactorCodeAsync(Guid userId)
        {
            var twoFactorAuth = await _context.TwoFactorAuth
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.UserId == userId && t.IsEnabled);

            if (twoFactorAuth == null)
            {
                return false;
            }

            // Generate a random 6-digit code
            var code = GenerateRandomCode();

            // Store the code in cache or session with expiration
            await StoreCodeInCache(userId, code);

            switch (twoFactorAuth.Type)
            {
                case TwoFactorType.Email:
                    await SendCodeByEmail(twoFactorAuth.User.Email, code);
                    break;
                case TwoFactorType.SMS:
                    await SendCodeBySms(twoFactorAuth.User.PhoneNumber, code);
                    break;
                case TwoFactorType.Authenticator:
                    // For authenticator, we don't need to send a code
                    return true;
                default:
                    return false;
            }

            return true;
        }

        public async Task<string> GetAuthenticatorKeyAsync(Guid userId)
        {
            var twoFactorAuth = await _context.TwoFactorAuth
                .FirstOrDefaultAsync(t => t.UserId == userId && t.Type == TwoFactorType.Authenticator);

            if (twoFactorAuth == null || string.IsNullOrEmpty(twoFactorAuth.SecretKey))
            {
                // Generate a new key if not exists
                var secretKey = GenerateSecretKey();
                
                if (twoFactorAuth == null)
                {
                    twoFactorAuth = new TwoFactorAuthModel
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        IsEnabled = false, // Not enabled until verified
                        Type = TwoFactorType.Authenticator,
                        SecretKey = secretKey,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _context.TwoFactorAuth.AddAsync(twoFactorAuth);
                }
                else
                {
                    twoFactorAuth.SecretKey = secretKey;
                }
                
                await _context.SaveChangesAsync();
                return secretKey;
            }

            return twoFactorAuth.SecretKey;
        }

        #region Helper Methods

        private string GenerateSecretKey()
        {
            var key = KeyGeneration.GenerateRandomKey(20);
            return Base32Encoding.ToString(key);
        }

        private bool VerifyAuthenticatorCode(string secretKey, string code)
        {
            if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(code))
            {
                return false;
            }

            try
            {
                var keyBytes = Base32Encoding.ToBytes(secretKey);
                var totp = new Totp(keyBytes);
                return totp.VerifyTotp(code, out long timeStepMatched);
            }
            catch
            {
                return false;
            }
        }

        private string GenerateRandomCode()
        {
            var random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        private async Task StoreCodeInCache(Guid userId, string code)
        {
            // In a real implementation, this would use a distributed cache or database
            // For simplicity, we're just simulating it here
            // This would be replaced with actual cache implementation
            
            // Example with a cache service:
            // await _cacheService.SetAsync($"2FA_CODE_{userId}", code, TimeSpan.FromMinutes(10));
        }

        private async Task<bool> VerifyCodeFromCache(Guid userId, string code)
        {
            // In a real implementation, this would check against a distributed cache or database
            // For simplicity, we're just simulating it here
            // This would be replaced with actual cache implementation
            
            // Example with a cache service:
            // var storedCode = await _cacheService.GetAsync<string>($"2FA_CODE_{userId}");
            // return storedCode == code;
            
            return true; // Simulated for now
        }

        private async Task SendCodeByEmail(string email, string code)
        {
            var subject = "Your AI_BACKUP Verification Code";
            var body = $"Your verification code is: {code}. It will expire in 10 minutes.";
            
            await _emailService.SendEmailAsync(email, subject, body);
        }

        private async Task SendCodeBySms(string phoneNumber, string code)
        {
            var message = $"Your AI_BACKUP verification code is: {code}. It will expire in 10 minutes.";
            
            await _smsService.SendSmsAsync(phoneNumber, message);
        }

        #endregion
    }

    public interface ITwoFactorAuthService
    {
        Task<bool> EnableTwoFactorAuthAsync(Guid userId, TwoFactorType type);
        Task<bool> DisableTwoFactorAuthAsync(Guid userId);
        Task<bool> VerifyTwoFactorCodeAsync(Guid userId, string code);
        Task<bool> SendTwoFactorCodeAsync(Guid userId);
        Task<string> GetAuthenticatorKeyAsync(Guid userId);
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string body);
    }

    public interface ISmsService
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}