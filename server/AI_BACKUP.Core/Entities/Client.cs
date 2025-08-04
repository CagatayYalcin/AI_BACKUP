using System;
using System.Collections.Generic;

namespace AI_BACKUP.Core.Entities
{
    public class Client
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

        // Navigation properties
        public User Owner { get; set; }
        public ICollection<BackupJob> BackupJobs { get; set; }
    }
}