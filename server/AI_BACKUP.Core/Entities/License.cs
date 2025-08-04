using System;

namespace AI_BACKUP.Core.Entities
{
    public class License
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string LicenseKey { get; set; }
        public DateTime ValidUntil { get; set; }
        public int MaxClients { get; set; }
        public double MaxStorageGb { get; set; }
        public double CurrentStorageGb { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public User User { get; set; }
    }
}