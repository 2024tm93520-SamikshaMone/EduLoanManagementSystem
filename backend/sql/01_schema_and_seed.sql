USE EduLoanDB;
GO

/* =========================================================
   ADD EMPLOYEES ONLY IF THEY DON'T ALREADY EXIST
   ========================================================= */

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeCode = 'EMP1005')
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1005',
        'Priya Sharma',
        'priya.sharma@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Engineering'),
        'Senior Software Engineer',
        '2021-04-12',
        85000,
        '+91 9876543214',
        1
    );
END;


IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeCode = 'EMP1006')
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1006',
        'Rahul Patil',
        'rahul.patil@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Engineering'),
        'Software Engineer',
        '2022-06-20',
        72000,
        '+91 9876543215',
        1
    );
END;


IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeCode = 'EMP1007')
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1007',
        'Sneha Kulkarni',
        'sneha.kulkarni@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Human Resources'),
        'HR Analyst',
        '2020-08-10',
        78000,
        '+91 9876543216',
        1
    );
END;


IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeCode = 'EMP1008')
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1008',
        'Amit Deshmukh',
        'amit.deshmukh@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Finance'),
        'Finance Analyst',
        '2019-03-18',
        90000,
        '+91 9876543217',
        1
    );
END;


IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE EmployeeCode = 'EMP1009')
BEGIN
    INSERT INTO dbo.Users
    (
        EmployeeCode, FullName, Email, PasswordHash,
        Role, DepartmentId, Designation, DateOfJoining,
        MonthlySalary, PhoneNumber, IsActive
    )
    VALUES
    (
        'EMP1009',
        'Neha Joshi',
        'neha.joshi@acc.com',
        '$2b$10$9YbUuqUOHgaN8IGX5fjxbOzHzd7UlNc2lrEtfrjW0XtudU2w9mD5O',
        'Employee',
        (SELECT Id FROM dbo.Department WHERE Name = 'Administration'),
        'Business Analyst',
        '2023-01-09',
        68000,
        '+91 9876543218',
        1
    );
END;

GO

/* =========================================================
   ADD LOAN APPLICATIONS ONLY IF APPLICATION NUMBER
   DOES NOT ALREADY EXIST
   ========================================================= */

IF NOT EXISTS (
    SELECT 1 FROM dbo.LoanApplication
    WHERE ApplicationNumber = 'ELA-2026-0005'
)
BEGIN
    INSERT INTO dbo.LoanApplication
    (
        ApplicationNumber,
        EmployeeId,
        CollegeId,
        CourseId,
        Specialization,
        CourseDurationMonths,
        TotalEducationFees,
        RequestedAmount,
        RequestedTenureMonths,
        EducationPurpose,
        Status,
        CreatedAt,
        SubmittedAt
    )
    VALUES
    (
        'ELA-2026-0005',
        (SELECT Id FROM dbo.Users WHERE EmployeeCode = 'EMP1005'),
        (SELECT Id FROM dbo.College
         WHERE Name = 'Birla Institute of Technology & Science, Pilani'),
        (SELECT Id FROM dbo.Course
         WHERE Name = 'M.Tech Software Engineering'),
        'Software Engineering',
        24,
        450000,
        350000,
        36,
        'Higher Education',
        'Submitted',
        '2026-09-01 10:15:00',
        '2026-09-02 11:30:00'
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.LoanApplication
    WHERE ApplicationNumber = 'ELA-2026-0006'
)
BEGIN
    INSERT INTO dbo.LoanApplication
    (
        ApplicationNumber, EmployeeId, CollegeId, CourseId,
        Specialization, CourseDurationMonths, TotalEducationFees,
        RequestedAmount, RequestedTenureMonths, EducationPurpose,
        Status, CreatedAt, SubmittedAt
    )
    VALUES
    (
        'ELA-2026-0006',
        (SELECT Id FROM dbo.Users WHERE EmployeeCode = 'EMP1006'),
        (SELECT Id FROM dbo.College
         WHERE Name = 'Indian Institute of Technology Bombay'),
        (SELECT Id FROM dbo.Course
         WHERE Name = 'M.Tech Software Engineering'),
        'Computer Science',
        24,
        500000,
        400000,
        48,
        'Higher Education',
        'Approved',
        '2026-08-10 09:20:00',
        '2026-08-11 10:15:00'
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.LoanApplication
    WHERE ApplicationNumber = 'ELA-2026-0007'
)
BEGIN
    INSERT INTO dbo.LoanApplication
    (
        ApplicationNumber, EmployeeId, CollegeId, CourseId,
        Specialization, CourseDurationMonths, TotalEducationFees,
        RequestedAmount, RequestedTenureMonths, EducationPurpose,
        Status, CreatedAt, SubmittedAt
    )
    VALUES
    (
        'ELA-2026-0007',
        (SELECT Id FROM dbo.Users WHERE EmployeeCode = 'EMP1007'),
        (SELECT Id FROM dbo.College
         WHERE Name = 'Savitribai Phule Pune University'),
        (SELECT Id FROM dbo.Course
         WHERE Name = 'MBA'),
        'Human Resources',
        24,
        320000,
        250000,
        36,
        'Higher Education',
        'Rejected',
        '2026-08-05 14:10:00',
        '2026-08-06 09:45:00'
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.LoanApplication
    WHERE ApplicationNumber = 'ELA-2026-0008'
)
BEGIN
    INSERT INTO dbo.LoanApplication
    (
        ApplicationNumber, EmployeeId, CollegeId, CourseId,
        Specialization, CourseDurationMonths, TotalEducationFees,
        RequestedAmount, RequestedTenureMonths, EducationPurpose,
        Status, CreatedAt, SubmittedAt
    )
    VALUES
    (
        'ELA-2026-0008',
        (SELECT Id FROM dbo.Users WHERE EmployeeCode = 'EMP1008'),
        (SELECT Id FROM dbo.College
         WHERE Name = 'Indian Institute of Technology Bombay'),
        (SELECT Id FROM dbo.Course
         WHERE Name = 'M.Tech Software Engineering'),
        'Data Engineering',
        24,
        480000,
        375000,
        48,
        'Higher Education',
        'Approved',
        '2026-07-15 11:00:00',
        '2026-07-16 12:20:00'
    );
END;


IF NOT EXISTS (
    SELECT 1 FROM dbo.LoanApplication
    WHERE ApplicationNumber = 'ELA-2026-0009'
)
BEGIN
    INSERT INTO dbo.LoanApplication
    (
        ApplicationNumber, EmployeeId, CollegeId, CourseId,
        Specialization, CourseDurationMonths, TotalEducationFees,
        RequestedAmount, RequestedTenureMonths, EducationPurpose,
        Status, CreatedAt, SubmittedAt
    )
    VALUES
    (
        'ELA-2026-0009',
        (SELECT Id FROM dbo.Users WHERE EmployeeCode = 'EMP1009'),
        (SELECT Id FROM dbo.College
         WHERE Name = 'Savitribai Phule Pune University'),
        (SELECT Id FROM dbo.Course
         WHERE Name = 'MBA'),
        'Finance',
        24,
        350000,
        275000,
        36,
        'Higher Education',
        'Submitted',
        '2026-09-10 15:30:00',
        '2026-09-11 10:00:00'
    );
END;

GO

SELECT EmployeeCode, FullName, Email
FROM dbo.Users
WHERE EmployeeCode BETWEEN 'EMP1005' AND 'EMP1009';

SELECT ApplicationNumber, Status
FROM dbo.LoanApplication
WHERE ApplicationNumber IN
(
    'ELA-2026-0005',
    'ELA-2026-0006',
    'ELA-2026-0007',
    'ELA-2026-0008',
    'ELA-2026-0009'
);

SELECT DISTINCT Status
FROM LoanApplication
ORDER BY Status;