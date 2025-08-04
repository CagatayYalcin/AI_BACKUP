import { User } from './user.model';

export enum TicketStatus {
  Open = 'Open',
  InProgress = 'InProgress',
  Resolved = 'Resolved',
  Closed = 'Closed'
}

export enum TicketPriority {
  Low = 'Low',
  Medium = 'Medium',
  High = 'High',
  Critical = 'Critical'
}

export interface TicketMessage {
  id: number;
  ticketId: number;
  userId: number;
  message: string;
  isInternal: boolean;
  createdAt: Date;
  user?: User;
}

export interface SupportTicket {
  id: number;
  ticketNumber: string;
  userId: number;
  subject: string;
  description: string;
  status: TicketStatus;
  priority: TicketPriority;
  assignedToUserId?: number;
  createdAt: Date;
  updatedAt?: Date;
  resolvedAt?: Date;
  user?: User;
  assignedToUser?: User;
  messages?: TicketMessage[];
}