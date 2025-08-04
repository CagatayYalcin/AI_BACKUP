export interface SubscriptionPlan {
  id: number;
  name: string;
  description: string;
  monthlyPrice: number;
  yearlyPrice: number;
  maxClients: number;
  maxStorageGb: number;
  isActive: boolean;
}