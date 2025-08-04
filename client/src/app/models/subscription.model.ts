import { SubscriptionPlan } from './subscription-plan.model';

export enum SubscriptionStatus {
  Active = 'Active',
  Expired = 'Expired',
  Cancelled = 'Cancelled',
  PendingPayment = 'PendingPayment'
}

export enum BillingCycle {
  Monthly = 'Monthly',
  Yearly = 'Yearly'
}

export interface Subscription {
  id: number;
  userId: number;
  subscriptionPlanId: number;
  status: SubscriptionStatus;
  billingCycle: BillingCycle;
  startDate: Date;
  endDate: Date;
  autoRenew: boolean;
  subscriptionPlan?: SubscriptionPlan;
}