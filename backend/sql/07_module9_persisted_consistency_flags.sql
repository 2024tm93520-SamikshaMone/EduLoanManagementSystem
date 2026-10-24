
/*
    Module 9: Persisted Consistency Flag Lifecycle
    Database: EduLoanDB

    Features:
      1. Persistent consistency flags
      2. Consistency flag review lifecycle
      3. Consistency flag history/audit trail
      4. Indexes for active flags, status, and history

    Migration behavior:
      - Additive and safe to rerun.
      - Does not drop existing tables or indexes.
      - Creates missing tables and indexes only.
*/

USE [EduLoanDB];
GO

SET NOCOUNT ON;
GO

/* =========================================================
   1. Validate prerequisite tables
   ========================================================= */

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    THROW 50020,
        'Prerequisite table dbo.Users was not found in EduLoanDB. Run the base EduLoan schema first.',
        1;
END;
GO

IF OBJECT_ID(N'dbo.LoanApplication', N'U') IS NULL
BEGIN
    THROW 50021,
        'Prerequisite table dbo.LoanApplication was not found in EduLoanDB. Verify the database and schema.',
        1;
END;
GO

/* =========================================================
   2. Create ConsistencyFlag
   ========================================================= */

IF OBJECT_ID(N'dbo.ConsistencyFlag', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsistencyFlag
    (
        Id uniqueidentifier NOT NULL
            CONSTRAINT PK_ConsistencyFlag PRIMARY KEY,

        ApplicationId uniqueidentifier NOT NULL,

        RuleCode nvarchar(80) NOT NULL,

        Title nvarchar(200) NOT NULL,

        Category nvarchar(50) NOT NULL,

        Severity nvarchar(20) NOT NULL,

        Description nvarchar(1000) NOT NULL,

        DetectedAtUtc datetime2 NOT NULL
            CONSTRAINT DF_ConsistencyFlag_DetectedAtUtc
            DEFAULT SYSUTCDATETIME(),

        Status nvarchar(30) NOT NULL
            CONSTRAINT DF_ConsistencyFlag_Status
            DEFAULT N'Open',

        ReviewedByUserId uniqueidentifier NULL,

        ReviewedAtUtc datetime2 NULL,

        ResolutionNotes nvarchar(1000) NULL,

        RowVersion rowversion NOT NULL,

        CONSTRAINT FK_ConsistencyFlag_LoanApplication
            FOREIGN KEY (ApplicationId)
            REFERENCES dbo.LoanApplication(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_ConsistencyFlag_Reviewer
            FOREIGN KEY (ReviewedByUserId)
            REFERENCES dbo.Users(Id)
            ON DELETE NO ACTION,

        CONSTRAINT CK_ConsistencyFlag_Status
            CHECK
            (
                Status IN
                (
                    N'Open',
                    N'UnderInvestigation',
                    N'Resolved',
                    N'Dismissed'
                )
            )
    );
END;
GO

/* =========================================================
   3. Create active consistency flag index
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlag')
      AND name = N'IX_ConsistencyFlag_ApplicationId_RuleCode_Active'
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.stats
        WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlag')
          AND name = N'IX_ConsistencyFlag_ApplicationId_RuleCode_Active'
    )
    BEGIN
        DROP STATISTICS
            dbo.ConsistencyFlag.IX_ConsistencyFlag_ApplicationId_RuleCode_Active;
    END;

    CREATE UNIQUE INDEX
        IX_ConsistencyFlag_ApplicationId_RuleCode_Active
    ON dbo.ConsistencyFlag
    (
        ApplicationId,
        RuleCode
    )
    WHERE Status IN (N'Open', N'UnderInvestigation');
END;
GO

/* =========================================================
   4. Create consistency flag status index
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlag')
      AND name = N'IX_ConsistencyFlag_Status_DetectedAtUtc'
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.stats
        WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlag')
          AND name = N'IX_ConsistencyFlag_Status_DetectedAtUtc'
    )
    BEGIN
        DROP STATISTICS
            dbo.ConsistencyFlag.IX_ConsistencyFlag_Status_DetectedAtUtc;
    END;

    CREATE INDEX IX_ConsistencyFlag_Status_DetectedAtUtc
    ON dbo.ConsistencyFlag
    (
        Status,
        DetectedAtUtc DESC
    );
END;
GO

/* =========================================================
   5. Create ConsistencyFlagHistory
   ========================================================= */

IF OBJECT_ID(N'dbo.ConsistencyFlagHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ConsistencyFlagHistory
    (
        Id uniqueidentifier NOT NULL
            CONSTRAINT PK_ConsistencyFlagHistory PRIMARY KEY,

        ConsistencyFlagId uniqueidentifier NOT NULL,

        ActorUserId uniqueidentifier NOT NULL,

        Action nvarchar(40) NOT NULL,

        Notes nvarchar(1000) NULL,

        CreatedAtUtc datetime2 NOT NULL
            CONSTRAINT DF_ConsistencyFlagHistory_CreatedAtUtc
            DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_ConsistencyFlagHistory_Flag
            FOREIGN KEY (ConsistencyFlagId)
            REFERENCES dbo.ConsistencyFlag(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_ConsistencyFlagHistory_Actor
            FOREIGN KEY (ActorUserId)
            REFERENCES dbo.Users(Id)
            ON DELETE NO ACTION
    );
END;
GO

/* =========================================================
   6. Create consistency flag history index
   ========================================================= */

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlagHistory')
      AND name = N'IX_ConsistencyFlagHistory_Flag_CreatedAtUtc'
)
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.stats
        WHERE object_id = OBJECT_ID(N'dbo.ConsistencyFlagHistory')
          AND name = N'IX_ConsistencyFlagHistory_Flag_CreatedAtUtc'
    )
    BEGIN
        DROP STATISTICS
            dbo.ConsistencyFlagHistory.IX_ConsistencyFlagHistory_Flag_CreatedAtUtc;
    END;

    CREATE INDEX IX_ConsistencyFlagHistory_Flag_CreatedAtUtc
    ON dbo.ConsistencyFlagHistory
    (
        ConsistencyFlagId,
        CreatedAtUtc
    );
END;
GO

/* =========================================================
   7. Verify created tables
   ========================================================= */

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName;

SELECT
    OBJECT_ID(N'dbo.Users', N'U') AS UsersTable,
    OBJECT_ID(N'dbo.LoanApplication', N'U') AS LoanApplicationTable,
    OBJECT_ID(N'dbo.ConsistencyFlag', N'U') AS ConsistencyFlagTable,
    OBJECT_ID(N'dbo.ConsistencyFlagHistory', N'U') AS ConsistencyFlagHistoryTable;
GO

/* =========================================================
   8. Verify required indexes
   ========================================================= */

SELECT
    OBJECT_SCHEMA_NAME(i.object_id) AS SchemaName,
    OBJECT_NAME(i.object_id) AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    i.has_filter AS HasFilter,
    i.filter_definition AS FilterDefinition
FROM sys.indexes AS i
WHERE
    (
        i.object_id = OBJECT_ID(N'dbo.ConsistencyFlag')
        AND i.name IN
        (
            N'IX_ConsistencyFlag_ApplicationId_RuleCode_Active',
            N'IX_ConsistencyFlag_Status_DetectedAtUtc'
        )
    )
    OR
    (
        i.object_id = OBJECT_ID(N'dbo.ConsistencyFlagHistory')
        AND i.name = N'IX_ConsistencyFlagHistory_Flag_CreatedAtUtc'
    )
ORDER BY
    TableName,
    IndexName;
GO