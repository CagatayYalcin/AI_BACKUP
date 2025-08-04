using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetUserByIdAsync(int id);
        Task<UserDto> GetUserByUsernameAsync(string username);
        Task<IReadOnlyList<UserDto>> GetAllUsersAsync();
        Task<UserDto> CreateUserAsync(CreateUserDto createUserDto);
        Task UpdateUserAsync(int id, UpdateUserDto updateUserDto);
        Task DeleteUserAsync(int id);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
    }
}