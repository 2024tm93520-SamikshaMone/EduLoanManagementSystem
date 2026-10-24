/* Additive migration: employment type is sourced from the employee master (Users) record.
   Existing values remain NULL until HR verifies the authoritative source. No values/defaults are invented.
   Run once against the target EduLoan database; safe to re-run. */
USE [EduLoanDB];
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
    THROW 50010, 'Prerequisite table dbo.Users was not found in EduLoanDB. Run the base EduLoan schema first or verify the database connection.', 1;
GO

IF COL_LENGTH(N'dbo.Users', N'EmploymentType') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD EmploymentType NVARCHAR(50) NULL;
END;
GO
SELECT DB_NAME() AS CurrentDatabase;

SELECT s.name AS SchemaName, t.name AS TableName
FROM sys.tables AS t
JOIN sys.schemas AS s ON s.schema_id = t.schema_id
WHERE t.name = N'Users';