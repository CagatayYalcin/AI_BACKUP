export enum StorageType {
  Local = 'Local',
  GoogleDrive = 'GoogleDrive',
  OneDrive = 'OneDrive',
  AmazonS3 = 'AmazonS3',
  AzureBlob = 'AzureBlob',
  FTP = 'FTP',
  SFTP = 'SFTP'
}

export interface StorageConfig {
  id: number;
  userId: number;
  name: string;
  description?: string;
  storageType: StorageType;
  isDefault: boolean;
  connectionString?: string;
  accessKey?: string;
  secretKey?: string;
  bucketName?: string;
  containerName?: string;
  folderPath?: string;
  region?: string;
  endpoint?: string;
  createdAt: Date;
  updatedAt?: Date;
}