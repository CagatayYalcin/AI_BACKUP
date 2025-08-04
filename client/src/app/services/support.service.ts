import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SupportTicket, TicketMessage, TicketPriority, TicketStatus } from '../models/support-ticket.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SupportService {
  private apiUrl = `${environment.apiUrl}/api/support`;

  constructor(private http: HttpClient) { }

  // User endpoints
  getUserTickets(): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiUrl}/tickets`);
  }

  getTicket(id: number): Observable<SupportTicket> {
    return this.http.get<SupportTicket>(`${this.apiUrl}/tickets/${id}`);
  }

  createTicket(subject: string, description: string, priority: TicketPriority): Observable<SupportTicket> {
    return this.http.post<SupportTicket>(`${this.apiUrl}/tickets`, {
      subject,
      description,
      priority
    });
  }

  getTicketMessages(ticketId: number): Observable<TicketMessage[]> {
    return this.http.get<TicketMessage[]>(`${this.apiUrl}/tickets/${ticketId}/messages`);
  }

  addTicketMessage(ticketId: number, message: string): Observable<TicketMessage> {
    return this.http.post<TicketMessage>(`${this.apiUrl}/tickets/${ticketId}/messages`, message);
  }

  // Support staff endpoints
  getAssignedTickets(): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiUrl}/staff/tickets`);
  }

  getAllTickets(): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiUrl}/staff/tickets/all`);
  }

  getTicketsByStatus(status: TicketStatus): Observable<SupportTicket[]> {
    return this.http.get<SupportTicket[]>(`${this.apiUrl}/staff/tickets/status/${status}`);
  }

  updateTicket(id: number, ticket: Partial<SupportTicket>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/staff/tickets/${id}`, ticket);
  }

  assignTicket(id: number, assignedToUserId: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/staff/tickets/${id}/assign`, assignedToUserId);
  }

  closeTicket(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/staff/tickets/${id}/close`, {});
  }

  reopenTicket(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/staff/tickets/${id}/reopen`, {});
  }
}