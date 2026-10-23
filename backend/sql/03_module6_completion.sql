USE [EduLoanDB];
GO

SET NOCOUNT ON;
GO

/*
    Module 6 additive schema changes.
    Prerequisites:
      1. 01_schema_and_seed.sql
      2. 02_password_reset_otp.sql

    Safe for reruns:
      - Does not drop existing tables or columns.
      - Does not delete existing data.
      - Adds missing tables, columns, indexes, and foreign keys.
      - Uses NO ACTION for the ApplicationDocument reference to avoid
        SQL Server multiple-cascade-path errors.
*/

IF DB_NAME() <> N'EduLoanDB'
    THROW 50001, 'Execute this script against EduLoanDB.', 1;
GO

IF OBJECT_ID(N'dbo.LoanApplication', N'U') IS NULL
    THROW 50002, 'Prerequisite table dbo.LoanApplication was not found.', 1;
GO

IF OBJECT_ID(N'dbo.ApplicationDocument', N'U') IS NULL
    THROW 50003, 'Prerequisite table dbo.ApplicationDocument was not found.', 1;
GO

/* ================================================================
   1. Extend ApplicationDocument
   ================================================================ */

IF COL_LENGTH(N'dbo.ApplicationDocument', N'UploadedByUserId') IS NULL
    ALTER TABLE dbo.ApplicationDocument
        ADD UploadedByUserId UNIQUEIDENTIFIER NULL;
GO

IF COL_LENGTH(N'dbo.ApplicationDocument', N'IsRequired') IS NULL
BEGIN
    ALTER TABLE dbo.ApplicationDocument
        ADD IsRequired BIT NOT NULL
            CONSTRAINT DF_ApplicationDocument_IsRequired
            DEFAULT (1);
END;
GO

IF COL_LENGTH(N'dbo.ApplicationDocument', N'VerificationStatus') IS NULL
BEGIN
    ALTER TABLE dbo.ApplicationDocument
        ADD VerificationStatus NVARCHAR(30) NOT NULL
            CONSTRAINT DF_ApplicationDocument_VerificationStatus
            DEFAULT (N'Uploaded');
END;
GO

IF COL_LENGTH(N'dbo.ApplicationDocument', N'VerifiedByUserId') IS NULL
    ALTER TABLE dbo.ApplicationDocument
        ADD VerifiedByUserId UNIQUEIDENTIFIER NULL;
GO

IF COL_LENGTH(N'dbo.ApplicationDocument', N'VerificationComments') IS NULL
    ALTER TABLE dbo.ApplicationDocument
        ADD VerificationComments NVARCHAR(1000) NULL;
GO

IF COL_LENGTH(N'dbo.ApplicationDocument', N'VerifiedAtUtc') IS NULL
    ALTER TABLE dbo.ApplicationDocument
        ADD VerifiedAtUtc DATETIME2 NULL;
GO

/* ================================================================
   2. InformationRequest
   ================================================================ */

IF OBJECT_ID(N'dbo.InformationRequest', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InformationRequest
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_InformationRequest PRIMARY KEY,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,
        RequestedByUserId UNIQUEIDENTIFIER NOT NULL,
        RequestedByName NVARCHAR(150) NOT NULL,
        Question NVARCHAR(1000) NOT NULL,
        RequiredDocumentType NVARCHAR(100) NULL,
        Reason NVARCHAR(500) NOT NULL,
        DueDateUtc DATETIME2 NULL,
        Instructions NVARCHAR(1000) NULL,

        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_InformationRequest_Status
            DEFAULT (N'Pending'),

        CreatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_InformationRequest_CreatedAt
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_InformationRequest_LoanApplication
            FOREIGN KEY (ApplicationId)
            REFERENCES dbo.LoanApplication(Id)
            ON DELETE CASCADE
    );
END;
GO

-- Create the index only if neither an equivalent index nor a conflicting
-- index/statistics name already exists. Existing statistics are never dropped.
IF OBJECT_ID(N'dbo.InformationRequest', N'U') IS NOT NULL
AND NOT EXISTS
(
    -- An equivalent two-column index already exists under any name.
    SELECT 1
    FROM sys.indexes AS i
    WHERE i.object_id = OBJECT_ID(N'dbo.InformationRequest', N'U')
      AND i.type = 2
      AND i.is_disabled = 0
      AND i.has_filter = 0
      AND
      (
          SELECT COUNT(*)
          FROM sys.index_columns AS ic
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal > 0
      ) = 2
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 1
            AND c.name = N'ApplicationId'
      )
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 2
            AND c.name = N'Status'
            AND ic.is_descending_key = 0
      )
)
AND NOT EXISTS
(
    -- SQL Server also reserves index names used by standalone statistics.
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InformationRequest', N'U')
    AND name = N'IX_InformationRequest_ApplicationId_Status'
)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.stats
    WHERE object_id = OBJECT_ID(N'dbo.InformationRequest', N'U')
            AND name = N'IX_InformationRequest_ApplicationId_Status'
)
BEGIN
        CREATE NONCLUSTERED INDEX IX_InformationRequest_ApplicationId_Status
        ON dbo.InformationRequest (ApplicationId, Status);
END;
GO

IF OBJECT_ID(N'dbo.InformationRequest', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.InformationRequest', N'U')
         AND name = N'FK_InformationRequest_LoanApplication'
   )
BEGIN
    ALTER TABLE dbo.InformationRequest WITH CHECK
        ADD CONSTRAINT FK_InformationRequest_LoanApplication
        FOREIGN KEY (ApplicationId)
        REFERENCES dbo.LoanApplication(Id)
        ON DELETE CASCADE;
END;
GO

/* ================================================================
   3. InformationRequestResponse
   ================================================================ */

IF OBJECT_ID(N'dbo.InformationRequestResponse', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InformationRequestResponse
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_InformationRequestResponse PRIMARY KEY,

        InformationRequestId UNIQUEIDENTIFIER NOT NULL,
        RespondedByUserId UNIQUEIDENTIFIER NOT NULL,
        RespondedByName NVARCHAR(150) NOT NULL,
        ResponseText NVARCHAR(2000) NOT NULL,
        DocumentFileName NVARCHAR(255) NULL,
        DocumentStoredFileName NVARCHAR(255) NULL,

        SubmittedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_InformationRequestResponse_SubmittedAt
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_InformationRequestResponse_Request
            FOREIGN KEY (InformationRequestId)
            REFERENCES dbo.InformationRequest(Id)
            ON DELETE CASCADE
    );
END;
GO

-- Create the index only if neither an equivalent index nor a conflicting
-- index/statistics name already exists. Existing statistics are never dropped.
IF OBJECT_ID(N'dbo.InformationRequestResponse', N'U') IS NOT NULL
AND NOT EXISTS
(
    -- An equivalent two-column index already exists under any name.
    SELECT 1
    FROM sys.indexes AS i
    WHERE i.object_id = OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
      AND i.type = 2
      AND i.is_disabled = 0
      AND i.has_filter = 0
      AND
      (
          SELECT COUNT(*)
          FROM sys.index_columns AS ic
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal > 0
      ) = 2
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 1
            AND c.name = N'InformationRequestId'
      )
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 2
            AND c.name = N'SubmittedAtUtc'
            AND ic.is_descending_key = 0
      )
)
AND NOT EXISTS
(
    -- SQL Server also reserves index names used by standalone statistics.
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
      AND name = N'IX_InformationRequestResponse_RequestId'
)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.stats
    WHERE object_id = OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
      AND name = N'IX_InformationRequestResponse_RequestId'
)
BEGIN
    BEGIN TRY
        CREATE NONCLUSTERED INDEX [IX_InformationRequestResponse_RequestId_InformationRequestId_SubmittedAtUtc]
            ON dbo.InformationRequestResponse (InformationRequestId, SubmittedAtUtc);
    END TRY
    BEGIN CATCH
        -- A concurrent deployment may create the index after the checks above.
        IF ERROR_NUMBER() <> 1913
            THROW;
    END CATCH;
END;
GO

IF OBJECT_ID(N'dbo.InformationRequestResponse', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id =
             OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
         AND name = N'FK_InformationRequestResponse_Request'
   )
BEGIN
    ALTER TABLE dbo.InformationRequestResponse WITH CHECK
        ADD CONSTRAINT FK_InformationRequestResponse_Request
        FOREIGN KEY (InformationRequestId)
        REFERENCES dbo.InformationRequest(Id)
        ON DELETE CASCADE;
END;
GO

/* ================================================================
   4. FinanceHandoff
   ================================================================ */

IF OBJECT_ID(N'dbo.FinanceHandoff', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FinanceHandoff
    (
        Id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_FinanceHandoff PRIMARY KEY,

        ApplicationId UNIQUEIDENTIFIER NOT NULL,
        ReceivedByUserId UNIQUEIDENTIFIER NULL,
        ReceivedAtUtc DATETIME2 NULL,
        BankReferenceNumber NVARCHAR(100) NULL,
        BankSubmissionDateUtc DATETIME2 NULL,
        ProcessedAmount DECIMAL(12,2) NULL,
        ConfirmationReference NVARCHAR(100) NULL,
        ConfirmationDateUtc DATETIME2 NULL,

        Status NVARCHAR(40) NOT NULL
            CONSTRAINT DF_FinanceHandoff_Status
            DEFAULT (N'Pending Finance Review'),

        FinanceComments NVARCHAR(1000) NULL,
        FailureReason NVARCHAR(500) NULL,

        CreatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_FinanceHandoff_CreatedAt
            DEFAULT (SYSUTCDATETIME()),

        UpdatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_FinanceHandoff_UpdatedAt
            DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT FK_FinanceHandoff_LoanApplication
            FOREIGN KEY (ApplicationId)
            REFERENCES dbo.LoanApplication(Id)
            ON DELETE CASCADE,

        CONSTRAINT UQ_FinanceHandoff_ApplicationId
            UNIQUE (ApplicationId)
    );
END;
GO

IF OBJECT_ID(N'dbo.FinanceHandoff', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id = OBJECT_ID(N'dbo.FinanceHandoff', N'U')
         AND name = N'FK_FinanceHandoff_LoanApplication'
   )
BEGIN
    ALTER TABLE dbo.FinanceHandoff WITH CHECK
        ADD CONSTRAINT FK_FinanceHandoff_LoanApplication
        FOREIGN KEY (ApplicationId)
        REFERENCES dbo.LoanApplication(Id)
        ON DELETE CASCADE;
END;
GO

IF OBJECT_ID(N'dbo.FinanceHandoff', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.key_constraints
       WHERE parent_object_id = OBJECT_ID(N'dbo.FinanceHandoff', N'U')
         AND name = N'UQ_FinanceHandoff_ApplicationId'
   )
BEGIN
    ALTER TABLE dbo.FinanceHandoff
        ADD CONSTRAINT UQ_FinanceHandoff_ApplicationId
        UNIQUE (ApplicationId);
END;
GO

/* ================================================================
   5. DocumentRequirement
   ================================================================ */

IF OBJECT_ID(N'dbo.DocumentRequirement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocumentRequirement
    (
        Id INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DocumentRequirement PRIMARY KEY,

        DocumentType NVARCHAR(100) NOT NULL,
        DisplayName NVARCHAR(150) NOT NULL,
        Category NVARCHAR(80) NOT NULL,

        IsRequired BIT NOT NULL
            CONSTRAINT DF_DocumentRequirement_IsRequired
            DEFAULT (1),

        MinimumLoanAmount DECIMAL(12,2) NULL,
        MaximumLoanAmount DECIMAL(12,2) NULL,
        EmploymentType NVARCHAR(50) NULL,
        EducationLevel NVARCHAR(50) NULL,

        RequiresCollateral BIT NOT NULL
            CONSTRAINT DF_DocumentRequirement_RequiresCollateral
            DEFAULT (0),

        IsActive BIT NOT NULL
            CONSTRAINT DF_DocumentRequirement_IsActive
            DEFAULT (1),

        DisplayOrder INT NOT NULL
            CONSTRAINT DF_DocumentRequirement_DisplayOrder
            DEFAULT (0),

        UpdatedAtUtc DATETIME2 NOT NULL
            CONSTRAINT DF_DocumentRequirement_UpdatedAt
            DEFAULT (SYSUTCDATETIME())
    );
END;
GO

-- Create the index only if neither an equivalent index nor a conflicting
-- index/statistics name already exists. Existing statistics are never dropped.
IF OBJECT_ID(N'dbo.DocumentRequirement', N'U') IS NOT NULL
AND NOT EXISTS
(
    -- An equivalent two-column index already exists under any name.
    SELECT 1
    FROM sys.indexes AS i
    WHERE i.object_id = OBJECT_ID(N'dbo.DocumentRequirement', N'U')
      AND i.type = 2
      AND i.is_disabled = 0
      AND i.has_filter = 0
      AND
      (
          SELECT COUNT(*)
          FROM sys.index_columns AS ic
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal > 0
      ) = 2
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 1
            AND c.name = N'IsActive'
      )
      AND EXISTS
      (
          SELECT 1
          FROM sys.index_columns AS ic
          INNER JOIN sys.columns AS c
              ON c.object_id = ic.object_id
             AND c.column_id = ic.column_id
          WHERE ic.object_id = i.object_id
            AND ic.index_id = i.index_id
            AND ic.key_ordinal = 2
            AND c.name = N'DisplayOrder'
            AND ic.is_descending_key = 0
      )
)
AND NOT EXISTS
(
    -- SQL Server also reserves index names used by standalone statistics.
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.DocumentRequirement', N'U')
      AND name = N'IX_DocumentRequirement_ActiveOrder'
)
AND NOT EXISTS
(
    SELECT 1
    FROM sys.stats
    WHERE object_id = OBJECT_ID(N'dbo.DocumentRequirement', N'U')
      AND name = N'IX_DocumentRequirement_ActiveOrder'
)
BEGIN
    EXEC(N'CREATE NONCLUSTERED INDEX IX_DocumentRequirement_ActiveOrder
        ON dbo.DocumentRequirement (IsActive, DisplayOrder);');
END;
GO

/* Seed defaults only when the table is empty. */
IF OBJECT_ID(N'dbo.DocumentRequirement', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.DocumentRequirement)
BEGIN
    INSERT INTO dbo.DocumentRequirement
        (DocumentType, DisplayName, Category, IsRequired, DisplayOrder)
    VALUES
        (N'PAN', N'PAN Card', N'Identity & Address Proof', 1, 10),
        (N'Aadhaar', N'Aadhaar Card', N'Identity & Address Proof', 1, 20),
        (N'Employee ID', N'Employee ID Card',
            N'Identity & Address Proof', 1, 30),
        (N'Admission Letter', N'Admission Letter',
            N'Academic & Admission Proof', 1, 40),
        (N'Fee Structure', N'University Fee Structure',
            N'Academic & Admission Proof', 1, 50),
        (N'Mark Sheets', N'Previous Mark Sheets',
            N'Academic & Admission Proof', 1, 60),
        (N'Salary Slips', N'Latest Salary Slips',
            N'Employment & Income Proof', 1, 70),
        (N'Bank Statement', N'Bank Statement',
            N'Employment & Income Proof', 0, 80),
        (N'Employment Certificate', N'Employment Certificate',
            N'Employment & Income Proof', 0, 90);
END;
GO

/* ================================================================
   6. Link InformationRequestResponse to ApplicationDocument
   ================================================================ */

IF OBJECT_ID(N'dbo.InformationRequestResponse', N'U') IS NOT NULL
   AND COL_LENGTH(
       N'dbo.InformationRequestResponse', N'ApplicationDocumentId'
   ) IS NULL
BEGIN
    ALTER TABLE dbo.InformationRequestResponse
        ADD ApplicationDocumentId UNIQUEIDENTIFIER NULL;
END;
GO

IF OBJECT_ID(N'dbo.InformationRequestResponse', N'U') IS NOT NULL
   AND COL_LENGTH(
       N'dbo.InformationRequestResponse', N'ApplicationDocumentId'
   ) IS NOT NULL
   AND OBJECT_ID(N'dbo.ApplicationDocument', N'U') IS NOT NULL
   AND NOT EXISTS
   (
       SELECT 1
       FROM sys.foreign_keys
       WHERE parent_object_id =
             OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
         AND name =
             N'FK_InformationRequestResponse_ApplicationDocument'
   )
BEGIN
    ALTER TABLE dbo.InformationRequestResponse WITH CHECK
        ADD CONSTRAINT FK_InformationRequestResponse_ApplicationDocument
        FOREIGN KEY (ApplicationDocumentId)
        REFERENCES dbo.ApplicationDocument(Id)
        ON DELETE NO ACTION;
END;
GO

/* ================================================================
   7. Verification
   ================================================================ */

SELECT
    DB_NAME() AS CurrentDatabase,
    OBJECT_ID(N'dbo.LoanApplication', N'U') AS LoanApplicationTableId,
    OBJECT_ID(N'dbo.ApplicationDocument', N'U') AS ApplicationDocumentTableId,
    OBJECT_ID(N'dbo.InformationRequest', N'U') AS InformationRequestTableId,
    OBJECT_ID(N'dbo.InformationRequestResponse', N'U')
        AS InformationRequestResponseTableId,
    OBJECT_ID(N'dbo.FinanceHandoff', N'U') AS FinanceHandoffTableId,
    OBJECT_ID(N'dbo.DocumentRequirement', N'U') AS DocumentRequirementTableId;
GO

SELECT
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS ChildTable,
    OBJECT_NAME(fk.referenced_object_id) AS ParentTable,
    fk.delete_referential_action_desc AS DeleteAction,
    fk.is_disabled AS IsDisabled,
    fk.is_not_trusted AS IsNotTrusted
FROM sys.foreign_keys AS fk
WHERE fk.name IN
(
    N'FK_InformationRequest_LoanApplication',
    N'FK_InformationRequestResponse_Request',
    N'FK_FinanceHandoff_LoanApplication',
    N'FK_InformationRequestResponse_ApplicationDocument'
)
ORDER BY fk.name;
GO