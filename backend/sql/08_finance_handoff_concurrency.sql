/* Module 10: optimistic concurrency for Finance handoff state transitions.
   Apply after 03_module6_completion.sql. Safe for existing rows; SQL Server
   generates a distinct rowversion value for each existing/new record. */
USE [EduLoanDB];
GO

IF OBJECT_ID(N'dbo.FinanceHandoff', N'U') IS NULL
    THROW 51000, 'FinanceHandoff table is missing. Apply 03_module6_completion.sql first.', 1;
GO
IF COL_LENGTH(N'dbo.FinanceHandoff', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.FinanceHandoff ADD RowVersion ROWVERSION NOT NULL;
END;
GO

IF COL_LENGTH(N'dbo.FinanceHandoff', N'RowVersion') IS NULL
    THROW 51001, 'FinanceHandoff.RowVersion was not created. Verify database permissions and rerun this script.', 1;
SELECT N'EduLoanDB' AS DatabaseName, COL_LENGTH(N'dbo.FinanceHandoff', N'RowVersion') AS RowVersionColumnLength;
GO

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    COL_LENGTH(N'dbo.FinanceHandoff', N'RowVersion') AS RowVersionColumn;