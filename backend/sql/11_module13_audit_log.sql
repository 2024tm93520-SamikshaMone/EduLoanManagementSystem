/* Module 13: append-only API audit trail. Additive and rerunnable. */
USE [EduLoanDB];
GO
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50031, 'Prerequisite dbo.Users was not found in EduLoanDB.', 1;
GO
IF OBJECT_ID(N'dbo.AuditLog', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLog
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AuditLog PRIMARY KEY,
        ActorUserId uniqueidentifier NULL,
        ActorRole nvarchar(20) NOT NULL,
        Action nvarchar(120) NOT NULL,
        EntityType nvarchar(120) NOT NULL,
        EntityId nvarchar(100) NULL,
        Outcome nvarchar(30) NOT NULL CONSTRAINT DF_AuditLog_Outcome DEFAULT N'Success',
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_AuditLog_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CorrelationId nvarchar(100) NULL,
        Metadata nvarchar(1000) NULL,
        CONSTRAINT FK_AuditLog_Users FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id) ON DELETE SET NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLog') AND name = N'IX_AuditLog_CreatedAtUtc')
    CREATE INDEX IX_AuditLog_CreatedAtUtc ON dbo.AuditLog(CreatedAtUtc DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLog') AND name = N'IX_AuditLog_Entity')
    CREATE INDEX IX_AuditLog_Entity ON dbo.AuditLog(EntityType, EntityId, CreatedAtUtc DESC);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuditLog') AND name = N'IX_AuditLog_Actor')
    CREATE INDEX IX_AuditLog_Actor ON dbo.AuditLog(ActorUserId, CreatedAtUtc DESC);
GO
SELECT OBJECT_ID(N'dbo.AuditLog', N'U') AS AuditLogTable;

SELECT DB_NAME() AS CurrentDatabase,
       OBJECT_ID(N'dbo.AuditLog', N'U') AS AuditLogTable;