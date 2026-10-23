USE [EduLoanDB];
GO
SET NOCOUNT ON;
GO

/*
  Module 7 support:
  - Keeps HR accounts in the database but the application maps HR logins to Admin.
  - KNN uses existing final Approved/Rejected applications; no training table or
    new dependency is needed.
  - Simplifies required document checklist to admission and fee evidence.
  - Existing uploaded documents and loan application rows are not deleted.
*/

IF OBJECT_ID(N'dbo.DocumentRequirement', N'U') IS NULL
    THROW 50020, 'Prerequisite table dbo.DocumentRequirement was not found. Run 03_module6_completion.sql first.', 1;
GO

/* Retire bank-style identity, income, employment and marksheet requirements.
   Keep the rows for history/configuration; do not delete uploaded files. */
UPDATE dbo.DocumentRequirement
SET IsActive = 0,
    IsRequired = 0,
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE LOWER(DocumentType) NOT IN (N'admission letter', N'fee structure', N'university fee structure');
GO

/* Ensure the two intended required evidence types are active. */
IF EXISTS (SELECT 1 FROM dbo.DocumentRequirement WHERE LOWER(DocumentType) = N'admission letter')
BEGIN
    UPDATE dbo.DocumentRequirement
    SET DisplayName = N'Admission Letter',
        Category = N'Education Admission & Fees',
        IsRequired = 1,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE LOWER(DocumentType) = N'admission letter';
END
ELSE
BEGIN
    INSERT dbo.DocumentRequirement
        (DocumentType, DisplayName, Category, IsRequired, IsActive, DisplayOrder, UpdatedAtUtc)
    VALUES
        (N'Admission Letter', N'Admission Letter', N'Education Admission & Fees', 1, 1, 10, SYSUTCDATETIME());
END;
GO

IF EXISTS (SELECT 1 FROM dbo.DocumentRequirement WHERE LOWER(DocumentType) = N'fee structure')
BEGIN
    UPDATE dbo.DocumentRequirement
    SET DisplayName = N'Fee Structure',
        Category = N'Education Admission & Fees',
        IsRequired = 1,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE LOWER(DocumentType) = N'fee structure';

    UPDATE dbo.DocumentRequirement
    SET IsRequired = 0, IsActive = 0, UpdatedAtUtc = SYSUTCDATETIME()
    WHERE LOWER(DocumentType) = N'university fee structure';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.DocumentRequirement WHERE LOWER(DocumentType) = N'university fee structure')
BEGIN
    UPDATE dbo.DocumentRequirement
    SET DocumentType = N'Fee Structure',
        DisplayName = N'Fee Structure',
        Category = N'Education Admission & Fees',
        IsRequired = 1,
        IsActive = 1,
        UpdatedAtUtc = SYSUTCDATETIME()
    WHERE LOWER(DocumentType) = N'university fee structure';
END
ELSE
BEGIN
    INSERT dbo.DocumentRequirement
        (DocumentType, DisplayName, Category, IsRequired, IsActive, DisplayOrder, UpdatedAtUtc)
    VALUES
        (N'Fee Structure', N'Fee Structure', N'Education Admission & Fees', 1, 1, 20, SYSUTCDATETIME());
END;
GO

SELECT Id, DocumentType, DisplayName, Category, IsRequired, IsActive, DisplayOrder
FROM dbo.DocumentRequirement
ORDER BY DisplayOrder, DisplayName;
GO
