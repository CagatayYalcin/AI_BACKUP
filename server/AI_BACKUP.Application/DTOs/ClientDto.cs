using System;

namespace AI_BACKUP.Application.DTOs
{
    public class ClientDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ClientId { get; set; }
        public string ApiKey { get; set; }
        public string OsType { get; set; }
        public string OsVersion { get; set; }
        public string IpAddress { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool IsActive { get; set; }
        public int OwnerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateClientDto
    {
        public string Name { get; set; }
        public string OsType { get; set; }
        public string OsVersion { get; set; }
        public string IpAddress { get; set; }
        public int OwnerId { get; set; }
    }

    public class UpdateClientDto
    {
        public string Name { get; set; }
        public string OsType { get; set; }
        public string OsVersion { get; set; }
        public string IpAddress { get; set; }
        public bool IsActive { get; set; }
    }
}