/* Module 9: persistent in-app notifications. Additive and safe to rerun. */
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
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Notification_UserId_ReadAtUtc_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.Notification'))
    CREATE INDEX IX_Notification_UserId_ReadAtUtc_CreatedAtUtc ON dbo.Notification(UserId, ReadAtUtc, CreatedAtUtc DESC);
