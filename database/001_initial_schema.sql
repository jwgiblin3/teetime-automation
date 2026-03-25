-- TeeTimeAutomator - Initial Schema Creation Script
-- Database: PostgreSQL (Supabase)

-- ============================================================================
-- TABLE: Users
-- ============================================================================
CREATE TABLE Users (
    UserId      SERIAL PRIMARY KEY,
    Email       VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255),
    FirstName   VARCHAR(100) NOT NULL,
    LastName    VARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20),
    GoogleOAuthId VARCHAR(255) UNIQUE,
    IsAdmin     BOOLEAN NOT NULL DEFAULT FALSE,
    IsActive    BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_Users_Email    ON Users(Email);
CREATE INDEX IX_Users_IsActive ON Users(IsActive);

-- ============================================================================
-- TABLE: Courses
-- ============================================================================
CREATE TABLE Courses (
    CourseId            SERIAL PRIMARY KEY,
    CourseName          VARCHAR(255) NOT NULL,
    BookingUrl          VARCHAR(500) NOT NULL,
    Platform            VARCHAR(50)  NOT NULL,
    ReleaseScheduleJson VARCHAR(1000) NOT NULL DEFAULT '{"daysInAdvance":14,"releaseTime":"06:00"}',
    IsActive            BOOLEAN NOT NULL DEFAULT TRUE,
    CreatedAt           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt           TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_Courses_CourseName ON Courses(CourseName);
CREATE INDEX IX_Courses_IsActive   ON Courses(IsActive);

-- ============================================================================
-- TABLE: UserCourseCredentials
-- ============================================================================
CREATE TABLE UserCourseCredentials (
    CredentialId      SERIAL PRIMARY KEY,
    UserId            INT NOT NULL REFERENCES Users(UserId)   ON DELETE CASCADE,
    CourseId          INT NOT NULL REFERENCES Courses(CourseId) ON DELETE RESTRICT,
    EncryptedEmail    VARCHAR(500) NOT NULL,
    EncryptedPassword VARCHAR(500) NOT NULL,
    CreatedAt         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT UQ_UserCourseCredentials UNIQUE (UserId, CourseId)
);

CREATE INDEX IX_UserCourseCredentials_UserId   ON UserCourseCredentials(UserId);
CREATE INDEX IX_UserCourseCredentials_CourseId ON UserCourseCredentials(CourseId);

-- ============================================================================
-- TABLE: BookingRequests
-- ============================================================================
CREATE TABLE BookingRequests (
    RequestId         SERIAL PRIMARY KEY,
    UserId            INT NOT NULL REFERENCES Users(UserId)   ON DELETE RESTRICT,
    CourseId          INT NOT NULL REFERENCES Courses(CourseId) ON DELETE RESTRICT,
    DesiredDate       DATE NOT NULL,
    PreferredTime     TIME NOT NULL,
    TimeWindowMinutes INT NOT NULL DEFAULT 60,
    NumberOfPlayers   INT NOT NULL DEFAULT 4,
    Status            VARCHAR(50) NOT NULL DEFAULT 'Pending',
    ScheduledFireTime TIMESTAMPTZ,
    HangfireJobId     VARCHAR(255),
    CreatedAt         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_BookingRequests_UserId_Status ON BookingRequests(UserId, Status);
CREATE INDEX IX_BookingRequests_Status        ON BookingRequests(Status);
CREATE INDEX IX_BookingRequests_ScheduledFireTime ON BookingRequests(ScheduledFireTime);
CREATE INDEX IX_BookingRequests_CourseId      ON BookingRequests(CourseId);

-- ============================================================================
-- TABLE: BookingResults
-- ============================================================================
CREATE TABLE BookingResults (
    ResultId           SERIAL PRIMARY KEY,
    RequestId          INT NOT NULL UNIQUE REFERENCES BookingRequests(RequestId) ON DELETE CASCADE,
    BookedTime         TIMESTAMPTZ,
    ConfirmationNumber VARCHAR(255),
    AttemptCount       INT NOT NULL DEFAULT 1,
    LastAttemptAt      TIMESTAMPTZ,
    FailureReason      VARCHAR(1000),
    IsSuccess          BOOLEAN NOT NULL DEFAULT FALSE,
    CreatedAt          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UpdatedAt          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_BookingResults_IsSuccess ON BookingResults(IsSuccess);

-- ============================================================================
-- TABLE: AuditLogs
-- ============================================================================
CREATE TABLE AuditLogs (
    LogId      SERIAL PRIMARY KEY,
    UserId     INT REFERENCES Users(UserId) ON DELETE SET NULL,
    RequestId  INT REFERENCES BookingRequests(RequestId) ON DELETE CASCADE,
    EventType  VARCHAR(100) NOT NULL,
    Message    VARCHAR(2000) NOT NULL,
    CreatedAt  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_AuditLogs_CreatedAt  ON AuditLogs(CreatedAt);
CREATE INDEX IX_AuditLogs_UserId     ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_RequestId  ON AuditLogs(RequestId);
CREATE INDEX IX_AuditLogs_EventType  ON AuditLogs(EventType);

-- ============================================================================
-- SEED DATA
-- ============================================================================

-- Admin user (password: "Admin123!" hashed with bcrypt)
INSERT INTO Users (Email, PasswordHash, FirstName, LastName, IsAdmin, IsActive)
VALUES ('admin@teetime.com', '$2a$11$8qXxnCpbVG8c/CxV2Kxh.eFOLGSdtGChVxHxS1UwhFhXxH9xP3/Vu', 'Admin', 'User', TRUE, TRUE);
