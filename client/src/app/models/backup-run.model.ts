import { BackupJob } from './backup-job.model';

export enum BackupRunStatus {
  Pending = 'Pending',
  Running = 'Running',
  Completed = 'Completed',
  Failed = 'Failed',
  Cancelled = 'Cancelled'
}

export interface BackupRun {
  id: number;
  backupJobId: number;
  status: BackupRunStatus;
  startTime: Date;
  endTime?: Date;
  sizeBytes: number;
  backupPath: string;
  errorMessage?: string;
  logPath?: string;
  createdAt: Date;
  backupJob?: BackupJob;
}