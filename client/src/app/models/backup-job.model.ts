import { Client } from './client.model';
import { StorageConfig } from './storage-config.model';

export enum BackupType {
  File = 'File',
  Directory = 'Directory',
  Database = 'Database'
}

export enum BackupStatus {
  Active = 'Active',
  Paused = 'Paused',
  Disabled = 'Disabled'
}

export enum DatabaseType {
  MSSQL = 'MSSQL',
  MySQL = 'MySQL',
  PostgreSQL = 'PostgreSQL',
  MongoDB = 'MongoDB',
  Oracle = 'Oracle'
}

export enum CompressionType {
  None = 'None',
  Zip = 'Zip',
  GZip = 'GZip',
  SevenZip = '7Zip'
}

export enum EncryptionType {
  None = 'None',
  AES128 = 'AES128',
  AES256 = 'AES256'
}

export enum ScheduleType {
  Manual = 'Manual',
  Daily = 'Daily',
  Weekly = 'Weekly',
  Monthly = 'Monthly',
  Custom = 'Custom'
}

export interface BackupJob {
  id: number;
  name: string;
  description?: string;
  clientId: number;
  storageConfigId: number;
  backupType: BackupType;
  status: BackupStatus;
  sourcePath: string;
  databaseType?: DatabaseType;
  databaseConnectionString?: string;
  compressionType: CompressionType;
  encryptionType: EncryptionType;
  encryptionPassword?: string;
  scheduleType: ScheduleType;
  scheduleExpression?: string;
  retentionDays: number;
  createdAt: Date;
  updatedAt?: Date;
  client?: Client;
  storageConfig?: StorageConfig;
}