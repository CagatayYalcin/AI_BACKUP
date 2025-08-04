import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { SubscriptionPlan } from '../models/subscription-plan.model';
import { Subscription, BillingCycle } from '../models/subscription.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class SubscriptionService {
  private apiUrl = `${environment.apiUrl}/api/subscriptions`;

  constructor(private http: HttpClient) { }

  // Public endpoints
  getActiveSubscriptionPlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${this.apiUrl}/plans`);
  }

  getSubscriptionPlan(id: number): Observable<SubscriptionPlan> {
    return this.http.get<SubscriptionPlan>(`${this.apiUrl}/plans/${id}`);
  }

  // User endpoints
  getUserSubscriptions(): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(`${this.apiUrl}`);
  }

  getSubscription(id: number): Observable<Subscription> {
    return this.http.get<Subscription>(`${this.apiUrl}/${id}`);
  }

  createSubscription(subscriptionPlanId: number, billingCycle: BillingCycle, autoRenew: boolean): Observable<Subscription> {
    return this.http.post<Subscription>(`${this.apiUrl}`, {
      subscriptionPlanId,
      billingCycle,
      autoRenew
    });
  }

  updateSubscription(id: number, subscription: Partial<Subscription>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, subscription);
  }

  cancelSubscription(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/cancel`, {});
  }

  renewSubscription(id: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/renew`, {});
  }

  // Admin endpoints
  getAllSubscriptionPlans(): Observable<SubscriptionPlan[]> {
    return this.http.get<SubscriptionPlan[]>(`${this.apiUrl}/admin/plans`);
  }

  createSubscriptionPlan(plan: Partial<SubscriptionPlan>): Observable<SubscriptionPlan> {
    return this.http.post<SubscriptionPlan>(`${this.apiUrl}/admin/plans`, plan);
  }

  updateSubscriptionPlan(id: number, plan: Partial<SubscriptionPlan>): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/admin/plans/${id}`, plan);
  }

  deleteSubscriptionPlan(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/admin/plans/${id}`);
  }

  getAllSubscriptions(): Observable<Subscription[]> {
    return this.http.get<Subscription[]>(`${this.apiUrl}/admin/all`);
  }
}