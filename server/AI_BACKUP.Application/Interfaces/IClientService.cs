using AI_BACKUP.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AI_BACKUP.Application.Interfaces
{
    public interface IClientService
    {
        Task<ClientDto> GetClientByIdAsync(int id);
        Task<ClientDto> GetClientByClientIdAsync(string clientId);
        Task<IReadOnlyList<ClientDto>> GetAllClientsAsync();
        Task<IReadOnlyList<ClientDto>> GetClientsByOwnerIdAsync(int ownerId);
        Task<ClientDto> CreateClientAsync(CreateClientDto createClientDto);
        Task UpdateClientAsync(int id, UpdateClientDto updateClientDto);
        Task DeleteClientAsync(int id);
        Task<ClientDto> AuthenticateClientAsync(string clientId, string apiKey);
        Task UpdateClientLastSeenAsync(int id, string ipAddress);
    }
}