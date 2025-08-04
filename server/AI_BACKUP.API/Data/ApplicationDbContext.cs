using AI_BACKUP.API.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace AI_BACKUP.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<StorageConfig> StorageConfigs { get; set; }
        public DbSet<BackupJob> BackupJobs { get; set; }
        public DbSet<BackupRun> BackupRuns { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<TicketMessage> TicketMessages { get; set; }
        public DbSet<TwoFactorAuthModel> TwoFactorAuth { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>()
                .HasMany(u => u.Clients)
                .WithOne(c => c.User)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.StorageConfigs)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Subscriptions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Payments)
                .WithOne(p => p.User)
                .HasForeignKey(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Invoices)
                .WithOne(i => i.User)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.SupportTickets)
                .WithOne(t => t.User)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Client entity
            modelBuilder.Entity<Client>()
                .HasMany(c => c.BackupJobs)
                .WithOne(b => b.Client)
                .HasForeignKey(b => b.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure BackupJob entity
            modelBuilder.Entity<BackupJob>()
                .HasMany(b => b.BackupRuns)
                .WithOne(r => r.BackupJob)
                .HasForeignKey(r => r.BackupJobId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SupportTicket entity
            modelBuilder.Entity<SupportTicket>()
                .HasMany(t => t.Messages)
                .WithOne(m => m.Ticket)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure TwoFactorAuth entity
            modelBuilder.Entity<TwoFactorAuthModel>()
                .HasOne(t => t.User)
                .WithOne()
                .HasForeignKey<TwoFactorAuthModel>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}