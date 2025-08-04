import { User } from './user.model';
import { Payment } from './payment.model';

export enum InvoiceStatus {
  Draft = 'Draft',
  Sent = 'Sent',
  Paid = 'Paid',
  Overdue = 'Overdue',
  Cancelled = 'Cancelled'
}

export interface Invoice {
  id: number;
  invoiceNumber: string;
  subscriptionId: number;
  userId: number;
  amount: number;
  currency: string;
  taxAmount: number;
  totalAmount: number;
  status: InvoiceStatus;
  issueDate: Date;
  dueDate: Date;
  billingAddress?: string;
  notes?: string;
  createdAt: Date;
  user?: User;
  payments?: Payment[];
}