using Hangfire;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TeeTimeAutomator.API.Jobs;

/// <summary>
/// Hangfire job that schedules booking requests to fire at the optimal time
/// based on course tee time release schedules
/// </summary>
public class ScheduleBookingJob
{
    private readonly ILogger<ScheduleBookingJob> _logger;
    private readonly AppDbContext _dbContext;

    public ScheduleBookingJob(
        ILogger<ScheduleBookingJob> logger,
        AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    /// <summary>
    /// Schedule a booking request to fire at the correct time based on course release schedule
    /// </summary>
    /// <param name="bookingRequestId">The ID of the booking request to schedule</param>
    public async Task ExecuteAsync(int bookingRequestId)
    {
        _logger.LogInformation("ScheduleBookingJob: Starting execution for booking request {BookingRequestId}", bookingRequestId);

        try
        {
            // Load booking request and course
            var bookingRequest = await _dbContext.BookingRequests
                .Include(b => b.Course)
                .FirstOrDefaultAsync(b => b.Id == bookingRequestId);

            if (bookingRequest == null)
            {
                _logger.LogError("Booking request {BookingRequestId} not found", bookingRequestId);
                return;
            }

            var course = bookingRequest.Course;
            if (course == null)
            {
                _logger.LogError("Course not found for booking request {BookingRequestId}", bookingRequestId);
                return;
            }

            _logger.LogInformation("Scheduling booking for course {CourseName} on date {PreferredDate}",
                course.Name, bookingRequest.PreferredDate);

            // Parse release schedule from course
            var releaseSchedule = ParseReleaseSchedule(course.ReleaseScheduleJson);

            if (releaseSchedule == null)
            {
                _logger.LogWarning("Invalid or missing release schedule for course {CourseId}, scheduling immediately", course.Id);
                releaseSchedule = new ReleaseSchedule { DaysInAdvance = 7, ReleaseHour = 8, ReleaseMinute = 0 };
            }

            // Calculate when the booking should fire
            var scheduleFireTime = CalculateScheduleFireTime(
                bookingRequest.PreferredDate,
                releaseSchedule);

            _logger.LogInformation("Calculated schedule fire time: {ScheduleFireTime} for booking request {BookingRequestId}",
                scheduleFireTime, bookingRequestId);

            // Update booking request with scheduled time
            bookingRequest.ScheduledFireTime = scheduleFireTime;
            bookingRequest.Status = BookingStatus.Scheduled;
            await _dbContext.SaveChangesAsync();

            // Schedule the BookTeeTimeJob to execute at the calculated time
            _logger.LogInformation("Scheduling BookTeeTimeJob to execute at {ScheduleFireTime}", scheduleFireTime);

            var jobId = BackgroundJob.Schedule<BookTeeTimeJob>(
                job => job.ExecuteAsync(bookingRequestId),
                scheduleFireTime);

            _logger.LogInformation("BookTeeTimeJob scheduled with ID {JobId} for booking request {BookingRequestId}",
                jobId, bookingRequestId);

            // Update booking request with job ID
            bookingRequest.HangfireJobId = jobId;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("ScheduleBookingJob completed successfully for booking request {BookingRequestId}", bookingRequestId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ScheduleBookingJob failed for booking request {BookingRequestId}", bookingRequestId);

            try
            {
                var bookingRequest = await _dbContext.BookingRequests.FindAsync(bookingRequestId);
                if (bookingRequest != null)
                {
                    bookingRequest.Status = BookingStatus.Failed;
                    bookingRequest.ErrorMessage = $"Failed to schedule booking: {ex.Message}";
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save error state for booking request {BookingRequestId}", bookingRequestId);
            }

            throw;
        }
    }

    /// <summary>
    /// Parse the JSON release schedule from the course
    /// </summary>
    private ReleaseSchedule? ParseReleaseSchedule(string? scheduleJson)
    {
        if (string.IsNullOrEmpty(scheduleJson))
        {
            _logger.LogWarning("Release schedule JSON is null or empty");
            return null;
        }

        try
        {
            _logger.LogDebug("Parsing release schedule JSON: {ScheduleJson}", scheduleJson);
            var schedule = JsonSerializer.Deserialize<ReleaseSchedule>(scheduleJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (schedule != null)
            {
                _logger.LogDebug("Successfully parsed release schedule: DaysInAdvance={DaysInAdvance}, ReleaseHour={ReleaseHour}, ReleaseMinute={ReleaseMinute}",
                    schedule.DaysInAdvance, schedule.ReleaseHour, schedule.ReleaseMinute);
            }

            return schedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse release schedule JSON");
            return null;
        }
    }

    /// <summary>
    /// Calculate when the booking job should fire based on the preferred date and release schedule
    /// </summary>
    private DateTime CalculateScheduleFireTime(DateTime preferredDate, ReleaseSchedule schedule)
    {
        // Calculate the release date (days before the preferred date)
        var releaseDate = preferredDate.AddDays(-schedule.DaysInAdvance);

        // Set the exact release time
        var releaseDateTime = releaseDate.Date
            .AddHours(schedule.ReleaseHour)
            .AddMinutes(schedule.ReleaseMinute);

        _logger.LogInformation("Release schedule: {DaysInAdvance} days in advance, at {ReleaseHour:D2}:{ReleaseMinute:D2}",
            schedule.DaysInAdvance, schedule.ReleaseHour, schedule.ReleaseMinute);

        _logger.LogInformation("Preferred date: {PreferredDate}, Release datetime: {ReleaseDateTime}",
            preferredDate.Date, releaseDateTime);

        // If the calculated time is in the past, schedule immediately
        if (releaseDateTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Calculated release time is in the past, scheduling for immediate execution");
            return DateTime.UtcNow.AddSeconds(1);
        }

        return releaseDateTime;
    }

    /// <summary>
    /// Release schedule configuration for a course
    /// </summary>
    private class ReleaseSchedule
    {
        /// <summary>
        /// Number of days in advance that tee times are released
        /// </summary>
        public int DaysInAdvance { get; set; } = 7;

        /// <summary>
        /// Hour of day when tee times are released (0-23)
        /// </summary>
        public int ReleaseHour { get; set; } = 8;

        /// <summary>
        /// Minute of hour when tee times are released (0-59)
        /// </summary>
        public int ReleaseMinute { get; set; } = 0;
    }
}
