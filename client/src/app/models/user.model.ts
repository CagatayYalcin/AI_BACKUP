export enum UserRole {
  Admin = 'Admin',
  User = 'User',
  Support = 'Support',
  Billing = 'Billing'
}

export interface User {
  id: number;
  email: string;
  username: string;
  fullName: string;
  role: UserRole;
  isActive: boolean;
  company?: string;
  address?: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  phone?: string;
  taxId?: string;
  createdAt: Date;
  updatedAt?: Date;
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
  fullName: string;
}

export interface AuthResponse {
  token: string;
  user: User;
  expiration: Date;
}