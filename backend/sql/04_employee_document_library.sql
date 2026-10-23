USE [EduLoanDB];
GO

SET NOCOUNT ON;
GO

-- Verify prerequisite table
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50010, 'Prerequisite table dbo.Users was not found.', 1;
GO

-- Create EmployeeDocument only if it does not exist
IF OBJECT_ID(N'dbo.EmployeeDocument', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmployeeDocument
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_EmployeeDocument PRIMARY KEY,

        EmployeeId UNIQUEIDENTIFIER NOT NULL,
        DocumentType NVARCHAR(100) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        StoredFileName NVARCHAR(255) NOT NULL,

        ContentType NVARCHAR(100) NOT NULL
            CONSTRAINT DF_EmployeeDocument_ContentType
            DEFAULT (N'application/octet-stream'),

        FileSizeBytes BIGINT NOT NULL,

        UploadedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_EmployeeDocument_UploadedAtUtc
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_EmployeeDocument_Users
            FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Users(Id)
            ON DELETE CASCADE
    );
END;
GO

-- Create an index only when an equivalent index does not exist.
-- Use a new name to avoid the existing name conflict.
IF OBJECT_ID(N'dbo.EmployeeDocument', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocument', N'U')
         AND index_id > 0
         AND is_disabled = 0
         AND has_filter = 0
         AND name IN
         (
             N'IX_EmployeeDocument_EmployeeId_UploadedAtUtc',
             N'IX_EmployeeDocument_EmployeeId_UploadedAtUtc_v2'
         )
   )
BEGIN
    CREATE INDEX IX_EmployeeDocument_EmployeeId_UploadedAtUtc_v2
        ON dbo.EmployeeDocument
        (
            EmployeeId,
            UploadedAtUtc DESC
        );
END;
GO

-- Verify table and indexes
SELECT
    DB_NAME() AS CurrentDatabase,
    OBJECT_ID(N'dbo.EmployeeDocument', N'U') AS EmployeeDocumentTableId;

SELECT
    name AS IndexName,
    type_desc AS IndexType,
    is_disabled AS IsDisabled
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.EmployeeDocument', N'U')
  AND name LIKE N'IX_EmployeeDocument_EmployeeId_UploadedAtUtc%';
GO
-- SELECT
--     s.name,
--     s.stats_id,
--     i.index_id,
--     i.name AS IndexName
-- FROM sys.stats AS s
-- LEFT JOIN sys.indexes AS i
--     ON i.object_id = s.object_id
--    AND i.index_id = s.stats_id
-- WHERE s.object_id = OBJECT_ID(N'dbo.EmployeeDocument', N'U')
--   AND s.name LIKE N'IX_EmployeeDocument_EmployeeId_UploadedAtUtc%';