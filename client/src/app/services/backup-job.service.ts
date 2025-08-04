import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BackupJob } from '../models/backup-job.model';
import { BackupRun } from '../models/backup-run.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BackupJobService {
  private apiUrl = `${environment.apiUrl}/api/backupjobs`;

  constructor(private http: HttpClient) { }

  getBackupJobs(): Observable<BackupJob[]> {
    return this.http.get<BackupJob[]>(`${this.apiUrl}`);
  }

  getBackupJob(id: number): Observable<BackupJob> {
    return this.http.get<BackupJob>(`${this.apiUrl}/${id}`);
  }

  createBackupJob(backupJob: Partial<BackupJob>): Observable<BackupJob> {
    return this.http.post<BackupJob>(`${this.apiUrl}`, backupJob);
  }

  updateBackupJob(id: number, backupJob: Partial<BackupJob>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, backupJob);
  }

  deleteBackupJob(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Backup runs
  getBackupRuns(backupJobId: number): Observable<BackupRun[]> {
    return this.http.get<BackupRun[]>(`${this.apiUrl}/${backupJobId}/runs`);
  }

  getRecentBackupRuns(): Observable<BackupRun[]> {
    return this.http.get<BackupRun[]>(`${this.apiUrl}/runs/recent`);
  }

  getBackupRun(backupJobId: number, runId: number): Observable<BackupRun> {
    return this.http.get<BackupRun>(`${this.apiUrl}/${backupJobId}/runs/${runId}`);
  }

  startBackupJob(id: number): Observable<BackupRun> {
    return this.http.post<BackupRun>(`${this.apiUrl}/${id}/run`, {});
  }

  cancelBackupRun(backupJobId: number, runId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${backupJobId}/runs/${runId}/cancel`, {});
  }

  // Job status management
  pauseBackupJob(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/pause`, {});
  }

  resumeBackupJob(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/resume`, {});
  }

  disableBackupJob(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/disable`, {});
  }
}