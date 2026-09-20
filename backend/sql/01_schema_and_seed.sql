-- ============================================================
-- EduLoanDB — Safe schema + seed data (Modules 1-4)
-- Database: EduLoanDB
-- SQL Server / Azure SQL Edge
--
-- IMPORTANT:
-- This script does NOT delete existing tables or data.
-- It is safe to execute multiple times.
-- ============================================================


-- ============================================================
-- 1. CREATE DATABASE
-- ============================================================

IF DB_ID('EduLoanDB') IS NULL
BEGIN
    CREATE DATABASE EduLoanDB;
END
GO

USE EduLoanDB;
GO


-- ============================================================
-- 2. DEPARTMENT
-- ============================================================

IF OBJECT_ID('dbo.Department', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Department
    (
        Id       INT IDENTITY(1,1) PRIMARY KEY,
        Name     NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO

-- Seed Department data only if it doesn't exist

IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Name = 'Engineering')
    INSERT INTO dbo.Department (Name) VALUES ('Engineering');

IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Name = 'Human Resources')
    INSERT INTO dbo.Department (Name) VALUES ('Human Resources');

IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Name = 'Finance')
    INSERT INTO dbo.Department (Name) VALUES ('Finance');

IF NOT EXISTS (SELECT 1 FROM dbo.Department WHERE Name = 'Administration')
    INSERT INTO dbo.Department (Name) VALUES ('Administration');
GO


-- ============================================================
-- 3. USERS
-- ============================================================

IF OBJECT_ID('dbo.Users', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        EmployeeCode    NVARCHAR(20) NOT NULL,
        FullName        NVARCHAR(150) NOT NULL,
        Email           NVARCHAR(150) NOT NULL,
        PasswordHash    NVARCHAR(255) NOT NULL,
        Role            NVARCHAR(20) NOT NULL,
        DepartmentId    INT NULL,
        Designation     NVARCHAR(100) NULL,
        DateOfJoining   DATE NOT NULL,
        MonthlySalary   DECIMAL(12,2) NOT NULL DEFAULT 0,
        PhoneNumber     NVARCHAR(15) NULL,
        IsActive        BIT NOT NULL DEFAULT 1,
        CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        UpdatedAt       DATETIME2 NULL,

        CONSTRAINT UQ_Users_EmployeeCode UNIQUE (EmployeeCode),
        CONSTRAINT UQ_Users_Email UNIQUE (Email),

        CONSTRAINT FK_Users_Department
            FOREIGN KEY (DepartmentId)
            REFERENCES dbo.Department(Id)
    );
END
GO


-- Seed Users

IF NOT EXISTS (
    SELECT 1 FROM dbo.Users
    WHERE EmployeeCode = 'EMP1001'
)
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1001',
        'Samiksha Mone',
        'samikshamone6@gmail.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Engineering'),
        'Software Engineer',
        '2022-01-10',
        65000,
        '+91 9876543210',
        1
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.Users
    WHERE EmployeeCode = 'EMP1002'
)
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1002',
        'Admin User',
        'admin@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Admin',
        (SELECT Id FROM dbo.Department WHERE Name = 'Administration'),
        'System Administrator',
        '2020-06-01',
        90000,
        '9876543211',
        1
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.Users
    WHERE EmployeeCode = 'EMP1003'
)
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1003',
        'HR User',
        'hr@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'HR',
        (SELECT Id FROM dbo.Department WHERE Name = 'Human Resources'),
        'HR Manager',
        '2019-03-15',
        80000,
        '9876543212',
        1
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.Users
    WHERE EmployeeCode = 'EMP1004'
)
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1004',
        'Finance User',
        'finance@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Finance',
        (SELECT Id FROM dbo.Department WHERE Name = 'Finance'),
        'Finance Executive',
        '2021-08-20',
        75000,
        '9876543213',
        1
    );
END;
GO


-- ============================================================
-- 4. COLLEGE
-- ============================================================

IF OBJECT_ID('dbo.College', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.College
    (
        Id       INT IDENTITY(1,1) PRIMARY KEY,
        Name     NVARCHAR(200) NOT NULL,
        City     NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL DEFAULT 1
    );
END
GO


-- Seed Colleges

IF NOT EXISTS (
    SELECT 1 FROM dbo.College
    WHERE Name = 'Birla Institute of Technology & Science, Pilani'
)
BEGIN
    INSERT INTO dbo.College (Name, City)
    VALUES
    ('Birla Institute of Technology & Science, Pilani', 'Pilani');
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.College
    WHERE Name = 'Indian Institute of Technology Bombay'
)
BEGIN
    INSERT INTO dbo.College (Name, City)
    VALUES
    ('Indian Institute of Technology Bombay', 'Mumbai');
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.College
    WHERE Name = 'Savitribai Phule Pune University'
)
BEGIN
    INSERT INTO dbo.College (Name, City)
    VALUES
    ('Savitribai Phule Pune University', 'Pune');
END;
GO


-- ============================================================
-- 5. COURSE
-- ============================================================

IF OBJECT_ID('dbo.Course', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Course
    (
        Id                      INT IDENTITY(1,1) PRIMARY KEY,
        Name                    NVARCHAR(150) NOT NULL,
        Level                   NVARCHAR(30) NOT NULL,
        StandardDurationMonths  INT NOT NULL,
        IsActive                BIT NOT NULL DEFAULT 1
    );
END
GO


-- Seed Courses

IF NOT EXISTS (
    SELECT 1 FROM dbo.Course
    WHERE Name = 'M.Tech Software Engineering'
)
BEGIN
    INSERT INTO dbo.Course
    (
        Name, Level, StandardDurationMonths
    )
    VALUES
    (
        'M.Tech Software Engineering', 'PG', 24
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.Course
    WHERE Name = 'MBA'
)
BEGIN
    INSERT INTO dbo.Course
    (
        Name, Level, StandardDurationMonths
    )
    VALUES
    (
        'MBA', 'PG', 24
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.Course
    WHERE Name = 'B.Tech Computer Science'
)
BEGIN
    INSERT INTO dbo.Course
    (
        Name, Level, StandardDurationMonths
    )
    VALUES
    (
        'B.Tech Computer Science', 'UG', 48
    );
END;
GO


-- ============================================================
-- 6. LOAN APPLICATION
-- ============================================================

IF OBJECT_ID('dbo.LoanApplication', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LoanApplication
    (
        Id                      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        ApplicationNumber       NVARCHAR(20) NOT NULL,
        EmployeeId              UNIQUEIDENTIFIER NOT NULL,
        CollegeId               INT NOT NULL,
        CourseId                INT NOT NULL,
        Specialization          NVARCHAR(150) NOT NULL,
        CourseDurationMonths    INT NOT NULL,
        TotalEducationFees      DECIMAL(12,2) NOT NULL,
        RequestedAmount         DECIMAL(12,2) NOT NULL,
        RequestedTenureMonths   INT NOT NULL,
        EducationPurpose        NVARCHAR(500) NOT NULL,
        Status                  NVARCHAR(20) NOT NULL DEFAULT 'Draft',
        CreatedAt               DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        SubmittedAt             DATETIME2 NULL,
        UpdatedAt               DATETIME2 NULL,

        CONSTRAINT UQ_LoanApplication_ApplicationNumber
            UNIQUE (ApplicationNumber),

        CONSTRAINT FK_LoanApplication_Employee
            FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Users(Id),

        CONSTRAINT FK_LoanApplication_College
            FOREIGN KEY (CollegeId)
            REFERENCES dbo.College(Id),

        CONSTRAINT FK_LoanApplication_Course
            FOREIGN KEY (CourseId)
            REFERENCES dbo.Course(Id)
    );
END
GO


-- ============================================================
-- 7. APPLICATION DOCUMENT
-- ============================================================

IF OBJECT_ID('dbo.ApplicationDocument', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApplicationDocument
    (
        Id             UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
        ApplicationId  UNIQUEIDENTIFIER NOT NULL,
        DocumentType   NVARCHAR(100) NOT NULL,
        FileName       NVARCHAR(255) NOT NULL,
        StoredFileName NVARCHAR(255) NOT NULL,
        ContentType    NVARCHAR(100) NOT NULL DEFAULT 'application/octet-stream',
        FileSizeBytes  BIGINT NOT NULL DEFAULT 0,
        UploadedAt     DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),

        CONSTRAINT FK_ApplicationDocument_Application
            FOREIGN KEY (ApplicationId)
            REFERENCES dbo.LoanApplication(Id)
            ON DELETE CASCADE
    );
END
GO


-- ============================================================
-- 8. VERIFICATION QUERIES
-- ============================================================

SELECT
    Id,
    EmployeeCode,
    FullName,
    Email,
    Role,
    IsActive
FROM dbo.Users;
GO

SELECT
    Id,
    ApplicationNumber,
    EmployeeId,
    CollegeId,
    CourseId,
    Status,
    CreatedAt,
    SubmittedAt
FROM dbo.LoanApplication
ORDER BY CreatedAt DESC;
GO

SELECT
    DB_NAME() AS CurrentDatabase,
    @@SERVERNAME AS ServerName;
GO

SELECT EmployeeCode, Email, PhoneNumber, UpdatedAt
FROM dbo.Users
WHERE EmployeeCode = 'EMP1001';