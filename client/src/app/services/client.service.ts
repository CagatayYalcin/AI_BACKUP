import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Client } from '../models/client.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class ClientService {
  private apiUrl = `${environment.apiUrl}/api/clients`;

  constructor(private http: HttpClient) { }

  getClients(): Observable<Client[]> {
    return this.http.get<Client[]>(`${this.apiUrl}`);
  }

  getClient(id: number): Observable<Client> {
    return this.http.get<Client>(`${this.apiUrl}/${id}`);
  }

  createClient(client: Partial<Client>): Observable<Client> {
    return this.http.post<Client>(`${this.apiUrl}`, client);
  }

  updateClient(id: number, client: Partial<Client>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, client);
  }

  deleteClient(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  // Client status management
  enableClient(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/enable`, {});
  }

  disableClient(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/disable`, {});
  }

  // Client installation
  getClientInstallationLink(id: number): Observable<string> {
    return this.http.get<string>(`${this.apiUrl}/${id}/installation-link`);
  }

  // Client statistics
  getClientStatistics(id: number): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}/statistics`);
  }
}