export enum ClientStatus {
  Online = 'Online',
  Offline = 'Offline',
  Maintenance = 'Maintenance',
  Disabled = 'Disabled'
}

export enum ClientType {
  WindowsServer = 'WindowsServer',
  Windows10 = 'Windows10',
  Windows11 = 'Windows11',
  Linux = 'Linux',
  MacOS = 'MacOS'
}

export interface Client {
  id: number;
  userId: number;
  name: string;
  description?: string;
  clientType: ClientType;
  status: ClientStatus;
  ipAddress?: string;
  macAddress?: string;
  hostname?: string;
  operatingSystem?: string;
  installedVersion?: string;
  lastSeenAt?: Date;
  storageUsageGb: number;
  createdAt: Date;
  updatedAt?: Date;
}