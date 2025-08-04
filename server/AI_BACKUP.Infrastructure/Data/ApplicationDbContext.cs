using AI_BACKUP.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AI_BACKUP.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<StorageConfig> StorageConfigs { get; set; }
        public DbSet<BackupJob> BackupJobs { get; set; }
        public DbSet<BackupRun> BackupRuns { get; set; }
        public DbSet<License> Licenses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure entity relationships and constraints here
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.ClientId)
                .IsUnique();

            modelBuilder.Entity<Client>()
                .HasIndex(c => c.ApiKey)
                .IsUnique();

            modelBuilder.Entity<License>()
                .HasIndex(l => l.LicenseKey)
                .IsUnique();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Auto-set CreatedAt and UpdatedAt fields
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity is User user)
                    {
                        user.CreatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is Client client)
                    {
                        client.CreatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is StorageConfig storageConfig)
                    {
                        storageConfig.CreatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is BackupJob backupJob)
                    {
                        backupJob.CreatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is BackupRun backupRun)
                    {
                        backupRun.CreatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is License license)
                    {
                        license.CreatedAt = DateTime.UtcNow;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    if (entry.Entity is User user)
                    {
                        user.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is Client client)
                    {
                        client.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is StorageConfig storageConfig)
                    {
                        storageConfig.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is BackupJob backupJob)
                    {
                        backupJob.UpdatedAt = DateTime.UtcNow;
                    }
                    else if (entry.Entity is License license)
                    {
                        license.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}