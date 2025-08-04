import { Component, OnInit } from '@angular/core';
import { Subscription } from '../../../models/subscription.model';
import { BackupJob } from '../../../models/backup-job.model';
import { BackupRun } from '../../../models/backup-run.model';
import { Client } from '../../../models/client.model';
import { SubscriptionService } from '../../../services/subscription.service';
import { BackupJobService } from '../../../services/backup-job.service';
import { ClientService } from '../../../services/client.service';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  activeSubscription: Subscription | null = null;
  recentBackupJobs: BackupJob[] = [];
  recentBackupRuns: BackupRun[] = [];
  clients: Client[] = [];
  storageUsage = 0;
  storageLimit = 0;
  clientCount = 0;
  clientLimit = 0;
  loading = {
    subscription: false,
    backupJobs: false,
    backupRuns: false,
    clients: false
  };
  error = {
    subscription: '',
    backupJobs: '',
    backupRuns: '',
    clients: ''
  };

  constructor(
    private subscriptionService: SubscriptionService,
    private backupJobService: BackupJobService,
    private clientService: ClientService
  ) { }

  ngOnInit(): void {
    this.loadActiveSubscription();
    this.loadRecentBackupJobs();
    this.loadRecentBackupRuns();
    this.loadClients();
  }

  loadActiveSubscription(): void {
    this.loading.subscription = true;
    this.subscriptionService.getUserSubscriptions().subscribe({
      next: (subscriptions) => {
        this.activeSubscription = subscriptions.find(s => s.status === 'Active') || null;
        if (this.activeSubscription && this.activeSubscription.subscriptionPlan) {
          this.storageLimit = this.activeSubscription.subscriptionPlan.maxStorageGb;
          this.clientLimit = this.activeSubscription.subscriptionPlan.maxClients;
        }
        this.loading.subscription = false;
      },
      error: (err) => {
        this.error.subscription = 'Failed to load subscription data';
        this.loading.subscription = false;
        console.error(err);
      }
    });
  }

  loadRecentBackupJobs(): void {
    this.loading.backupJobs = true;
    this.backupJobService.getBackupJobs().subscribe({
      next: (jobs) => {
        this.recentBackupJobs = jobs.slice(0, 5);
        this.loading.backupJobs = false;
      },
      error: (err) => {
        this.error.backupJobs = 'Failed to load backup jobs';
        this.loading.backupJobs = false;
        console.error(err);
      }
    });
  }

  loadRecentBackupRuns(): void {
    this.loading.backupRuns = true;
    this.backupJobService.getRecentBackupRuns().subscribe({
      next: (runs) => {
        this.recentBackupRuns = runs.slice(0, 5);
        this.loading.backupRuns = false;
      },
      error: (err) => {
        this.error.backupRuns = 'Failed to load backup runs';
        this.loading.backupRuns = false;
        console.error(err);
      }
    });
  }

  loadClients(): void {
    this.loading.clients = true;
    this.clientService.getClients().subscribe({
      next: (clients) => {
        this.clients = clients;
        this.clientCount = clients.length;
        this.calculateStorageUsage(clients);
        this.loading.clients = false;
      },
      error: (err) => {
        this.error.clients = 'Failed to load clients';
        this.loading.clients = false;
        console.error(err);
      }
    });
  }

  calculateStorageUsage(clients: Client[]): void {
    this.storageUsage = clients.reduce((total, client) => {
      return total + (client.storageUsageGb || 0);
    }, 0);
  }

  getStorageUsagePercentage(): number {
    if (this.storageLimit === 0) return 0;
    return Math.min(100, (this.storageUsage / this.storageLimit) * 100);
  }

  getClientUsagePercentage(): number {
    if (this.clientLimit === 0) return 0;
    return Math.min(100, (this.clientCount / this.clientLimit) * 100);
  }
}