import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Payment, PaymentRequest, PaymentResponse } from '../models/payment.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private apiUrl = `${environment.apiUrl}/api/payments`;

  constructor(private http: HttpClient) { }

  // User endpoints
  getUserPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.apiUrl}`);
  }

  getPayment(id: number): Observable<Payment> {
    return this.http.get<Payment>(`${this.apiUrl}/${id}`);
  }

  initiatePayment(paymentRequest: PaymentRequest): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`${this.apiUrl}/initiate`, paymentRequest);
  }

  // Admin endpoints
  getAllPayments(): Observable<Payment[]> {
    return this.http.get<Payment[]>(`${this.apiUrl}/admin/all`);
  }

  updatePayment(id: number, payment: Partial<Payment>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/admin/${id}`, payment);
  }

  refundPayment(id: number, amount: number): Observable<PaymentResponse> {
    return this.http.post<PaymentResponse>(`${this.apiUrl}/admin/${id}/refund`, amount);
  }
}