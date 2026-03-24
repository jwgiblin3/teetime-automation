-- TeeTimeAutomator - Initial Schema Creation Script
-- Database: TeeTimeAutomator
-- SQL Server 2019+

-- ============================================================================
-- TABLE: Users
-- Description: Stores user account information and authentication data
-- ============================================================================
CREATE TABLE [dbo].[Users] (
    [UserId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Email] NVARCHAR(255) NOT NULL UNIQUE,
    [PasswordHash] NVARCHAR(MAX) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [IsAdmin] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Users_Email ON [dbo].[Users]([Email]);
CREATE INDEX IX_Users_IsActive ON [dbo].[Users]([IsActive]);

-- ============================================================================
-- TABLE: Courses
-- Description: Golf course information and platform details
-- ============================================================================
CREATE TABLE [dbo].[Courses] (
    [CourseId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [Name] NVARCHAR(255) NOT NULL,
    [Location] NVARCHAR(255) NOT NULL,
    [Platform] NVARCHAR(50) NOT NULL, -- 'CPS Golf', 'GolfNow', 'TeeSnap', 'ForeUp'
    [PlatformCourseId] NVARCHAR(255) NOT NULL,
    [Holes] INT NOT NULL DEFAULT 18,
    [Par] INT NOT NULL,
    [Slope] INT,
    [Rating] DECIMAL(4, 1),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_Courses_Platform ON [dbo].[Courses]([Platform]);
CREATE INDEX IX_Courses_IsActive ON [dbo].[Courses]([IsActive]);
CREATE UNIQUE INDEX IX_Courses_PlatformId ON [dbo].[Courses]([Platform], [PlatformCourseId]);

-- ============================================================================
-- TABLE: UserCourseCredentials
-- Description: Platform-specific login credentials for each user at each course
-- ============================================================================
CREATE TABLE [dbo].[UserCourseCredentials] (
    [CredentialId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [CourseId] UNIQUEIDENTIFIER NOT NULL,
    [PlatformUsername] NVARCHAR(255) NOT NULL,
    [PlatformPassword] NVARCHAR(MAX) NOT NULL, -- Should be encrypted in production
    [HandicapIndex] DECIMAL(5, 1),
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_UserCourseCredentials_UserId FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
    CONSTRAINT FK_UserCourseCredentials_CourseId FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([CourseId]) ON DELETE CASCADE,
    CONSTRAINT UQ_UserCourseCredentials UNIQUE ([UserId], [CourseId])
);

CREATE INDEX IX_UserCourseCredentials_UserId ON [dbo].[UserCourseCredentials]([UserId]);
CREATE INDEX IX_UserCourseCredentials_CourseId ON [dbo].[UserCourseCredentials]([CourseId]);

-- ============================================================================
-- TABLE: BookingRequests
-- Description: User's booking requests with preferences and automation rules
-- ============================================================================
CREATE TABLE [dbo].[BookingRequests] (
    [RequestId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [CourseId] UNIQUEIDENTIFIER NOT NULL,
    [PreferredPlayDates] NVARCHAR(MAX), -- JSON array of dates in ISO format
    [PreferredStartTimes] NVARCHAR(MAX), -- JSON array of start times
    [NumberOfPlayers] INT NOT NULL DEFAULT 4,
    [BookingWindowStart] INT NOT NULL, -- Days ahead to start checking
    [BookingWindowEnd] INT NOT NULL, -- Days ahead to stop checking
    [MinimumGolfers] INT NOT NULL DEFAULT 2,
    [MaximumGolfers] INT NOT NULL DEFAULT 4,
    [CourseHolePreference] NVARCHAR(50), -- '9F', '9B', '18', NULL for any
    [WeekdayOnly] BIT NOT NULL DEFAULT 0,
    [WeekendOnly] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BookingRequests_UserId FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
    CONSTRAINT FK_BookingRequests_CourseId FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([CourseId]) ON DELETE CASCADE
);

CREATE INDEX IX_BookingRequests_UserId ON [dbo].[BookingRequests]([UserId]);
CREATE INDEX IX_BookingRequests_CourseId ON [dbo].[BookingRequests]([CourseId]);
CREATE INDEX IX_BookingRequests_IsActive ON [dbo].[BookingRequests]([IsActive]);

-- ============================================================================
-- TABLE: BookingResults
-- Description: Records of automated booking attempts and results
-- ============================================================================
CREATE TABLE [dbo].[BookingResults] (
    [ResultId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [RequestId] UNIQUEIDENTIFIER NOT NULL,
    [UserId] UNIQUEIDENTIFIER NOT NULL,
    [CourseId] UNIQUEIDENTIFIER NOT NULL,
    [AttemptedDate] DATE NOT NULL,
    [AttemptedStartTime] NVARCHAR(50) NOT NULL,
    [AttemptedNumberOfPlayers] INT NOT NULL,
    [Status] NVARCHAR(50) NOT NULL, -- 'Success', 'Failed', 'NoAvailability', 'Error'
    [ConfirmationNumber] NVARCHAR(255),
    [PlatformBookingId] NVARCHAR(255),
    [ErrorMessage] NVARCHAR(MAX),
    [BookingPrice] DECIMAL(10, 2),
    [AttemptedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [CompletedAt] DATETIME2,
    CONSTRAINT FK_BookingResults_RequestId FOREIGN KEY ([RequestId]) REFERENCES [dbo].[BookingRequests]([RequestId]) ON DELETE CASCADE,
    CONSTRAINT FK_BookingResults_UserId FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE CASCADE,
    CONSTRAINT FK_BookingResults_CourseId FOREIGN KEY ([CourseId]) REFERENCES [dbo].[Courses]([CourseId]) ON DELETE CASCADE
);

CREATE INDEX IX_BookingResults_UserId ON [dbo].[BookingResults]([UserId]);
CREATE INDEX IX_BookingResults_CourseId ON [dbo].[BookingResults]([CourseId]);
CREATE INDEX IX_BookingResults_Status ON [dbo].[BookingResults]([Status]);
CREATE INDEX IX_BookingResults_AttemptedDate ON [dbo].[BookingResults]([AttemptedDate]);

-- ============================================================================
-- TABLE: AuditLogs
-- Description: Tracks all system actions for compliance and debugging
-- ============================================================================
CREATE TABLE [dbo].[AuditLogs] (
    [LogId] UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    [UserId] UNIQUEIDENTIFIER,
    [Action] NVARCHAR(255) NOT NULL,
    [EntityType] NVARCHAR(100) NOT NULL,
    [EntityId] NVARCHAR(255),
    [OldValues] NVARCHAR(MAX), -- JSON representation of old values
    [NewValues] NVARCHAR(MAX), -- JSON representation of new values
    [IpAddress] NVARCHAR(45),
    [UserAgent] NVARCHAR(MAX),
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_AuditLogs_UserId FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]) ON DELETE SET NULL
);

CREATE INDEX IX_AuditLogs_UserId ON [dbo].[AuditLogs]([UserId]);
CREATE INDEX IX_AuditLogs_EntityType ON [dbo].[AuditLogs]([EntityType]);
CREATE INDEX IX_AuditLogs_CreatedAt ON [dbo].[AuditLogs]([CreatedAt]);

-- ============================================================================
-- SEED DATA
-- ============================================================================

-- Seed admin user (password hash is for "Admin123!" using bcrypt)
INSERT INTO [dbo].[Users] ([UserId], [Email], [PasswordHash], [FirstName], [LastName], [IsAdmin], [IsActive])
VALUES (NEWID(), 'admin@teetime.com', '$2a$11$8qXxnCpbVG8c/CxV2Kxh.eFOLGSdtGChVxHxS1UwhFhXxH9xP3/Vu', 'Admin', 'User', 1, 1);

-- Seed golf courses
INSERT INTO [dbo].[Courses] ([CourseId], [Name], [Location], [Platform], [PlatformCourseId], [Holes], [Par], [Slope], [Rating], [IsActive])
VALUES
    (NEWID(), 'Paramus Golf Course', 'Paramus, NJ', 'CPS Golf', 'paramus-001', 18, 72, 133, 73.2, 1),
    (NEWID(), 'Sunnybrook Golf Club', 'Radnor, PA', 'GolfNow', 'golfnow-sb-123', 18, 71, 130, 72.5, 1),
    (NEWID(), 'Eagle Ridge Golf Course', 'Denver, CO', 'TeeSnap', 'teesnap-er-456', 18, 72, 135, 73.8, 1),
    (NEWID(), 'Meadowbrook Country Club', 'Chicago, IL', 'ForeUp', 'foreup-mcb-789', 18, 72, 128, 72.1, 1);
