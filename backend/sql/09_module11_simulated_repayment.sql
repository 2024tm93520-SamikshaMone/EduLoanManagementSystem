/* Module 11 demo-only simulation. Apply after 01_schema_and_seed.sql and all existing module scripts.
   This schema records a manually verified activation event and simulated ledger entries only.
   It does not transfer funds, call a bank/payment processor, or represent a real payment. */
USE [EduLoanDB];
GO

IF OBJECT_ID(N'dbo.LoanActivationRecord', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoanActivationRecord
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_LoanActivationRecord PRIMARY KEY,
        ApplicationId uniqueidentifier NOT NULL,
        RecordedByUserId uniqueidentifier NOT NULL,
        ActivationDateUtc datetime2 NOT NULL,
        ActivationReference nvarchar(120) NOT NULL,
        PrincipalAmount decimal(12,2) NOT NULL,
        AnnualInterestRatePercent decimal(6,3) NOT NULL,
        TenureMonths int NOT NULL,
        Notes nvarchar(1000) NOT NULL,
        IsSimulation bit NOT NULL CONSTRAINT DF_LoanActivationRecord_IsSimulation DEFAULT (1),
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_LoanActivationRecord_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_LoanActivationRecord_Application UNIQUE (ApplicationId),
        CONSTRAINT FK_LoanActivationRecord_LoanApplication FOREIGN KEY (ApplicationId) REFERENCES dbo.LoanApplication(Id),
        CONSTRAINT FK_LoanActivationRecord_Users FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_LoanActivationRecord_Simulation CHECK (IsSimulation = 1),
        CONSTRAINT CK_LoanActivationRecord_Values CHECK (PrincipalAmount > 0 AND AnnualInterestRatePercent >= 0 AND AnnualInterestRatePercent <= 100 AND TenureMonths BETWEEN 1 AND 480)
    );
END;
GO
IF OBJECT_ID(N'dbo.SimulatedRepaymentInstallment', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SimulatedRepaymentInstallment
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_SimulatedRepaymentInstallment PRIMARY KEY,
        ActivationRecordId uniqueidentifier NOT NULL,
        InstallmentNumber int NOT NULL,
        DueDateUtc datetime2 NOT NULL,
        ScheduledAmount decimal(12,2) NOT NULL,
        SimulatedAmountPaid decimal(12,2) NOT NULL CONSTRAINT DF_SimulatedInstallment_Paid DEFAULT (0),
        SimulatedPaidAtUtc datetime2 NULL,
        Status nvarchar(20) NOT NULL CONSTRAINT DF_SimulatedInstallment_Status DEFAULT (N'Scheduled'),
        SimulationNote nvarchar(500) NULL,
        RecordedByUserId uniqueidentifier NULL,
        CreatedAtUtc datetime2 NOT NULL CONSTRAINT DF_SimulatedInstallment_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        RowVersion rowversion NOT NULL,
        CONSTRAINT UQ_SimulatedInstallment_Number UNIQUE (ActivationRecordId, InstallmentNumber),
        CONSTRAINT FK_SimulatedInstallment_Activation FOREIGN KEY (ActivationRecordId) REFERENCES dbo.LoanActivationRecord(Id) ON DELETE CASCADE,
        CONSTRAINT FK_SimulatedInstallment_Users FOREIGN KEY (RecordedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_SimulatedInstallment_Amounts CHECK (ScheduledAmount > 0 AND SimulatedAmountPaid >= 0 AND SimulatedAmountPaid <= ScheduledAmount),
        CONSTRAINT CK_SimulatedInstallment_Status CHECK (Status IN (N'Scheduled', N'PartiallyPaid', N'Paid'))
    );
    CREATE INDEX IX_SimulatedRepaymentInstallment_DueDate ON dbo.SimulatedRepaymentInstallment(DueDateUtc, Status);
END;
GO

SELECT
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
  AND (
      TABLE_NAME LIKE '%LoanApplication%'
      OR TABLE_NAME LIKE '%Application%'
  )
ORDER BY TABLE_SCHEMA, TABLE_NAME;

USE [EduLoanDB];
GO

SELECT
    OBJECT_ID(N'dbo.LoanApplication', N'U') AS LoanApplicationTable,
    OBJECT_ID(N'dbo.LoanActivationRecord', N'U') AS ActivationTable,
    OBJECT_ID(N'dbo.SimulatedRepaymentInstallment', N'U') AS InstallmentTable;
GO

SELECT TOP (20) *
FROM dbo.LoanApplication;

SELECT TOP (20) *
FROM dbo.LoanActivationRecord;

SELECT TOP (20) *
FROM dbo.SimulatedRepaymentInstallment;
GO