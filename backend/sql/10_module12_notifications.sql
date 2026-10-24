/* Module 12: notification API/UI support.
   Additive and safe to rerun. Reuses the existing Module 9 dbo.Notification table.
   Run against EduLoanDB after the base schema and Module 9 migration.
*/
USE [EduLoanDB];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50120, 'Prerequisite table dbo.Users was not found in EduLoanDB.', 1;
GO

IF OBJECT_ID(N'dbo.Notification', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notification
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_Notification PRIMARY KEY,
        UserId uniqueidentifier NOT NULL,
        Title nvarchar(160) NOT NULL,
        Message nvarchar(1000) NOT NULL,
        Link nvarchar(300) NULL,
        Category nvarchar(40) NOT NULL CONSTRAINT DF_Notification_Category DEFAULT N'General',
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_Notification_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        ReadAtUtc datetime2 NULL,
        CONSTRAINT FK_Notification_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Notification') AND name = N'IX_Notification_UserId_ReadAtUtc_CreatedAtUtc')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.stats WHERE object_id = OBJECT_ID(N'dbo.Notification') AND name = N'IX_Notification_UserId_ReadAtUtc_CreatedAtUtc')
        DROP STATISTICS dbo.Notification.IX_Notification_UserId_ReadAtUtc_CreatedAtUtc;
    CREATE INDEX IX_Notification_UserId_ReadAtUtc_CreatedAtUtc
        ON dbo.Notification(UserId, ReadAtUtc, CreatedAtUtc DESC);
END;
GO

SELECT DB_NAME() AS DatabaseName,
       OBJECT_ID(N'dbo.Notification', N'U') AS NotificationTable;
SELECT name AS IndexName FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.Notification')
  AND name = N'IX_Notification_UserId_ReadAtUtc_CreatedAtUtc';
GO
