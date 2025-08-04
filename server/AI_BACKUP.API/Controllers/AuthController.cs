using AI_BACKUP.Application.DTOs;
using AI_BACKUP.Application.Interfaces;
using AI_BACKUP.API.Models;
using AI_BACKUP.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace AI_BACKUP.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITwoFactorAuthService _twoFactorAuthService;
        private readonly IConfiguration _configuration;

        public AuthController(
            IUserService userService,
            ITwoFactorAuthService twoFactorAuthService,
            IConfiguration configuration)
        {
            _userService = userService;
            _twoFactorAuthService = twoFactorAuthService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<ActionResult<AuthResponseDto>> Login(LoginDto loginDto)
        {
            var response = await _userService.LoginAsync(loginDto);
            if (response == null)
            {
                return Unauthorized();
            }

            // Check if 2FA is enabled for the user
            var user = await _userService.GetUserByEmailAsync(loginDto.Email);
            if (user != null && user.TwoFactorEnabled)
            {
                // Send 2FA code
                await _twoFactorAuthService.SendTwoFactorCodeAsync(user.Id);
                
                return Ok(new
                {
                    requiresTwoFactor = true,
                    userId = user.Id
                });
            }

            return Ok(response);
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(CreateUserDto createUserDto)
        {
            var user = await _userService.CreateUserAsync(createUserDto);
            return CreatedAtAction(nameof(Register), new { id = user.Id }, user);
        }

        [HttpPost("google-login")]
        public async Task<ActionResult<AuthResponseDto>> GoogleLogin(ExternalLoginDto externalLoginDto)
        {
            var response = await _userService.ExternalLoginAsync(externalLoginDto);
            if (response == null)
            {
                return Unauthorized();
            }

            // Check if 2FA is enabled for the user
            var user = await _userService.GetUserByEmailAsync(externalLoginDto.Email);
            if (user != null && user.TwoFactorEnabled)
            {
                // Send 2FA code
                await _twoFactorAuthService.SendTwoFactorCodeAsync(user.Id);
                
                return Ok(new
                {
                    requiresTwoFactor = true,
                    userId = user.Id
                });
            }

            return Ok(response);
        }

        [HttpPost("verify-2fa")]
        public async Task<ActionResult<AuthResponseDto>> VerifyTwoFactorCode(TwoFactorVerificationDto verificationDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var isValid = await _twoFactorAuthService.VerifyTwoFactorCodeAsync(verificationDto.UserId, verificationDto.Code);
            if (!isValid)
            {
                return Unauthorized(new { message = "Invalid verification code" });
            }

            // Generate token and return response
            var response = await _userService.GenerateAuthResponseForUserAsync(verificationDto.UserId);
            if (response == null)
            {
                return Unauthorized();
            }

            return Ok(response);
        }

        [Authorize]
        [HttpGet("2fa-status")]
        public async Task<IActionResult> GetTwoFactorStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new { isEnabled = user.TwoFactorEnabled });
        }

        [Authorize]
        [HttpPost("enable-2fa")]
        public async Task<IActionResult> EnableTwoFactor(EnableTwoFactorDto model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var result = await _twoFactorAuthService.EnableTwoFactorAuthAsync(Guid.Parse(userId), (TwoFactorType)model.Type);
            if (!result)
            {
                return BadRequest(new { message = "Failed to enable two-factor authentication" });
            }

            // Update user's two-factor enabled flag
            await _userService.UpdateTwoFactorEnabledAsync(Guid.Parse(userId), true);

            return Ok(new { message = "Two-factor authentication enabled successfully" });
        }

        [Authorize]
        [HttpPost("disable-2fa")]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var result = await _twoFactorAuthService.DisableTwoFactorAuthAsync(Guid.Parse(userId));
            if (!result)
            {
                return BadRequest(new { message = "Failed to disable two-factor authentication" });
            }

            // Update user's two-factor enabled flag
            await _userService.UpdateTwoFactorEnabledAsync(Guid.Parse(userId), false);

            return Ok(new { message = "Two-factor authentication disabled successfully" });
        }

        [Authorize]
        [HttpGet("authenticator-key")]
        public async Task<IActionResult> GetAuthenticatorKey()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
            if (user == null)
            {
                return NotFound();
            }

            var key = await _twoFactorAuthService.GetAuthenticatorKeyAsync(Guid.Parse(userId));
            
            // Generate QR code URL
            var qrCodeUrl = $"otpauth://totp/AI_BACKUP:{user.Email}?secret={key}&issuer=AI_BACKUP";
            
            return Ok(new
            {
                key,
                qrCodeUrl
            });
        }
    }
}