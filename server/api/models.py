from sqlalchemy import Boolean, Column, ForeignKey, Integer, String, DateTime, Text, Float, Enum
from sqlalchemy.orm import relationship
from sqlalchemy.sql import func
import enum
from .database import Base

class UserRole(enum.Enum):
    ADMIN = "admin"
    USER = "user"

class BackupType(enum.Enum):
    FILE = "file"
    FOLDER = "folder"
    MSSQL = "mssql"
    MYSQL = "mysql"
    POSTGRESQL = "postgresql"

class StorageType(enum.Enum):
    LOCAL = "local"
    AWS_S3 = "aws_s3"
    AZURE_BLOB = "azure_blob"
    GOOGLE_CLOUD = "google_cloud"

class BackupStatus(enum.Enum):
    PENDING = "pending"
    IN_PROGRESS = "in_progress"
    COMPLETED = "completed"
    FAILED = "failed"

class User(Base):
    __tablename__ = "users"

    id = Column(Integer, primary_key=True, index=True)
    email = Column(String, unique=True, index=True)
    username = Column(String, unique=True, index=True)
    hashed_password = Column(String)
    full_name = Column(String)
    role = Column(Enum(UserRole), default=UserRole.USER)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    clients = relationship("Client", back_populates="owner")
    backup_jobs = relationship("BackupJob", back_populates="owner")
    storage_configs = relationship("StorageConfig", back_populates="owner")

class Client(Base):
    __tablename__ = "clients"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, index=True)
    client_id = Column(String, unique=True, index=True)
    api_key = Column(String, unique=True)
    os_type = Column(String)
    os_version = Column(String)
    ip_address = Column(String)
    last_seen = Column(DateTime(timezone=True))
    is_active = Column(Boolean, default=True)
    owner_id = Column(Integer, ForeignKey("users.id"))
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    owner = relationship("User", back_populates="clients")
    backup_jobs = relationship("BackupJob", back_populates="client")

class StorageConfig(Base):
    __tablename__ = "storage_configs"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, index=True)
    storage_type = Column(Enum(StorageType))
    config = Column(Text)  # JSON configuration for the storage
    owner_id = Column(Integer, ForeignKey("users.id"))
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    owner = relationship("User", back_populates="storage_configs")
    backup_jobs = relationship("BackupJob", back_populates="storage_config")

class BackupJob(Base):
    __tablename__ = "backup_jobs"

    id = Column(Integer, primary_key=True, index=True)
    name = Column(String, index=True)
    backup_type = Column(Enum(BackupType))
    source_path = Column(String)  # File/folder path or database connection string
    schedule = Column(String)  # Cron expression
    encrypt = Column(Boolean, default=False)
    compress = Column(Boolean, default=True)
    retention_days = Column(Integer, default=30)
    owner_id = Column(Integer, ForeignKey("users.id"))
    client_id = Column(Integer, ForeignKey("clients.id"))
    storage_config_id = Column(Integer, ForeignKey("storage_configs.id"))
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())

    owner = relationship("User", back_populates="backup_jobs")
    client = relationship("Client", back_populates="backup_jobs")
    storage_config = relationship("StorageConfig", back_populates="backup_jobs")
    backup_runs = relationship("BackupRun", back_populates="backup_job")

class BackupRun(Base):
    __tablename__ = "backup_runs"

    id = Column(Integer, primary_key=True, index=True)
    backup_job_id = Column(Integer, ForeignKey("backup_jobs.id"))
    status = Column(Enum(BackupStatus), default=BackupStatus.PENDING)
    start_time = Column(DateTime(timezone=True))
    end_time = Column(DateTime(timezone=True))
    size_bytes = Column(Integer)
    file_count = Column(Integer)
    error_message = Column(Text)
    destination_path = Column(String)
    created_at = Column(DateTime(timezone=True), server_default=func.now())

    backup_job = relationship("BackupJob", back_populates="backup_runs")

class License(Base):
    __tablename__ = "licenses"

    id = Column(Integer, primary_key=True, index=True)
    user_id = Column(Integer, ForeignKey("users.id"))
    license_key = Column(String, unique=True)
    valid_until = Column(DateTime(timezone=True))
    max_clients = Column(Integer)
    max_storage_gb = Column(Float)
    current_storage_gb = Column(Float, default=0)
    is_active = Column(Boolean, default=True)
    created_at = Column(DateTime(timezone=True), server_default=func.now())
    updated_at = Column(DateTime(timezone=True), onupdate=func.now())