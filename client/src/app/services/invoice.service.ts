import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Invoice } from '../models/invoice.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class InvoiceService {
  private apiUrl = `${environment.apiUrl}/api/invoices`;

  constructor(private http: HttpClient) { }

  // User endpoints
  getUserInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.apiUrl}`);
  }

  getInvoice(id: number): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.apiUrl}/${id}`);
  }

  downloadInvoicePdf(id: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${id}/pdf`, { responseType: 'blob' });
  }

  // Admin endpoints
  getAllInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(`${this.apiUrl}/admin/all`);
  }

  createInvoice(invoice: Partial<Invoice>): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.apiUrl}/admin`, invoice);
  }

  updateInvoice(id: number, invoice: Partial<Invoice>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/admin/${id}`, invoice);
  }

  deleteInvoice(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/admin/${id}`);
  }

  sendInvoice(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/admin/${id}/send`, {});
  }

  markInvoiceAsPaid(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/admin/${id}/mark-paid`, {});
  }

  markInvoiceAsOverdue(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/admin/${id}/mark-overdue`, {});
  }
}