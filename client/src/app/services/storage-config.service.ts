import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { StorageConfig } from '../models/storage-config.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class StorageConfigService {
  private apiUrl = `${environment.apiUrl}/api/storage-configs`;

  constructor(private http: HttpClient) { }

  getStorageConfigs(): Observable<StorageConfig[]> {
    return this.http.get<StorageConfig[]>(`${this.apiUrl}`);
  }

  getStorageConfig(id: number): Observable<StorageConfig> {
    return this.http.get<StorageConfig>(`${this.apiUrl}/${id}`);
  }

  createStorageConfig(storageConfig: Partial<StorageConfig>): Observable<StorageConfig> {
    return this.http.post<StorageConfig>(`${this.apiUrl}`, storageConfig);
  }

  updateStorageConfig(id: number, storageConfig: Partial<StorageConfig>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, storageConfig);
  }

  deleteStorageConfig(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  setDefaultStorageConfig(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/set-default`, {});
  }

  // Storage provider authentication
  getGoogleDriveAuthUrl(): Observable<string> {
    return this.http.get<string>(`${this.apiUrl}/auth/google-drive`);
  }

  getOneDriveAuthUrl(): Observable<string> {
    return this.http.get<string>(`${this.apiUrl}/auth/onedrive`);
  }

  completeGoogleDriveAuth(code: string): Observable<StorageConfig> {
    return this.http.post<StorageConfig>(`${this.apiUrl}/auth/google-drive/callback`, { code });
  }

  completeOneDriveAuth(code: string): Observable<StorageConfig> {
    return this.http.post<StorageConfig>(`${this.apiUrl}/auth/onedrive/callback`, { code });
  }

  // Storage testing
  testStorageConnection(id: number): Observable<{ success: boolean, message: string }> {
    return this.http.post<{ success: boolean, message: string }>(`${this.apiUrl}/${id}/test-connection`, {});
  }
}