-- TeeTimeAutomator - Hangfire Background Job Tables
-- Database: TeeTimeAutomator
-- SQL Server 2019+

-- ============================================================================
-- IMPORTANT NOTE
-- ============================================================================
-- Hangfire automatically creates its own set of tables when the application
-- first runs and connects to the database. These tables include:
--   - HangfireCounter
--   - HangfireHash
--   - HangfireJob
--   - HangfireJobParameter
--   - HangfireJobQueue
--   - HangfireList
--   - HangfireServer
--   - HangfireSet
--   - HangfireState
--
-- This file documents the automatic schema creation and provides recommended
-- indexes for performance optimization in production environments.
-- ============================================================================

-- ============================================================================
-- RECOMMENDED CUSTOM INDEXES FOR HANGFIRE PERFORMANCE
-- ============================================================================
-- These indexes should be added AFTER Hangfire creates its tables to optimize
-- query performance for common operations:

-- Index for polling job queue (background job processing)
CREATE INDEX IX_HangfireJobQueue_FetchedAt_Queue
ON [HangfireJobQueue]([FetchedAt], [Queue])
INCLUDE ([JobId]);

-- Index for recurring job lookups
CREATE INDEX IX_HangfireJob_StateName
ON [HangfireJob]([StateName])
INCLUDE ([CreatedAt]);

-- Index for state history queries
CREATE INDEX IX_HangfireState_JobId_CreatedAt
ON [HangfireState]([JobId], [CreatedAt])
INCLUDE ([Name]);

-- Index for server heartbeat monitoring
CREATE INDEX IX_HangfireServer_LastHeartbeat
ON [HangfireServer]([LastHeartbeat]);

-- ============================================================================
-- HANGFIRE CONFIGURATION NOTES
-- ============================================================================
-- In the TeeTimeAutomator.API project, Hangfire is configured in the
-- dependency injection setup with the following:
--
-- services.AddHangfire(config =>
--     config.UseSqlServerStorage(connectionString));
-- services.AddHangfireServer();
--
-- The automated booking jobs are scheduled using:
--   - RecurringJob.AddOrUpdate for daily/weekly scheduled runs
--   - BackgroundJob.Enqueue for on-demand booking requests
--   - BackgroundJob.ContinueJobWith for chained operations
--
-- Job retention is set to 1 hour for succeeded jobs and 7 days for failed jobs
-- to balance storage and diagnostic needs.
-- ============================================================================
