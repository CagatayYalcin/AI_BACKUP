export enum PaymentMethod {
  CreditCard = 'CreditCard',
  PayPal = 'PayPal',
  BankTransfer = 'BankTransfer',
  Other = 'Other'
}

export enum PaymentStatus {
  Pending = 'Pending',
  Completed = 'Completed',
  Failed = 'Failed',
  Refunded = 'Refunded'
}

export interface Payment {
  id: number;
  subscriptionId: number;
  invoiceId?: number;
  amount: number;
  currency: string;
  paymentMethod: PaymentMethod;
  status: PaymentStatus;
  transactionId?: string;
  paymentDetails?: string;
  paymentDate: Date;
  createdAt: Date;
}

export interface PaymentRequest {
  subscriptionId: number;
  invoiceId?: number;
  amount: number;
  currency: string;
  paymentMethod: PaymentMethod;
  returnUrl: string;
  cancelUrl: string;
}

export interface PaymentResponse {
  success: boolean;
  message: string;
  redirectUrl?: string;
  transactionId?: string;
  status: PaymentStatus;
}