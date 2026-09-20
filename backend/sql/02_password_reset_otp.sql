USE EduLoanDB;
GO

IF OBJECT_ID('dbo.PasswordResetOtp', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetOtp
    (
        Id UNIQUEIDENTIFIER NOT NULL
            DEFAULT NEWID()
            PRIMARY KEY,

        UserId UNIQUEIDENTIFIER NOT NULL,

        Email NVARCHAR(150) NOT NULL,

        OtpHash NVARCHAR(255) NOT NULL,

        ExpiresAtUtc DATETIME2 NOT NULL,

        Attempts INT NOT NULL DEFAULT 0,

        IsUsed BIT NOT NULL DEFAULT 0,

        CreatedAtUtc DATETIME2 NOT NULL
            DEFAULT SYSUTCDATETIME(),

        VerifiedAtUtc DATETIME2 NULL,

        ResetTokenHash NVARCHAR(255) NULL,

        ResetTokenExpiresAtUtc DATETIME2 NULL,

        CONSTRAINT FK_PasswordResetOtp_Users
            FOREIGN KEY (UserId)
            REFERENCES dbo.Users(Id)
    );
END
GO
