using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TeeTimeAutomator.API.Data;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Jobs;

/// <summary>
/// Hangfire job that schedules booking requests to fire at the optimal time
/// based on course tee time release schedules.
/// </summary>
public class ScheduleBookingJob
{
    private readonly ILogger<ScheduleBookingJob> _logger;
    private readonly AppDbContext _dbContext;

    public ScheduleBookingJob(ILogger<ScheduleBookingJob> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task ExecuteAsync(int bookingRequestId)
    {
        _logger.LogInformation("ScheduleBookingJob: Starting for booking request {BookingRequestId}", bookingRequestId);

        try
        {
            var bookingRequest = await _dbContext.BookingRequests
                .Include(b => b.Course)
                .FirstOrDefaultAsync(b => b.RequestId == bookingRequestId);

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

            _logger.LogInformation("Scheduling booking for course {CourseName} on {DesiredDate}",
                course.CourseName, bookingRequest.DesiredDate);

            var releaseSchedule = ParseReleaseSchedule(course.ReleaseScheduleJson)
                ?? new ReleaseSchedule { DaysInAdvance = 7, ReleaseHour = 8, ReleaseMinute = 0 };

            var scheduleFireTime = CalculateScheduleFireTime(bookingRequest.DesiredDate, releaseSchedule);

            _logger.LogInformation("Calculated schedule fire time: {ScheduleFireTime}", scheduleFireTime);

            bookingRequest.ScheduledFireTime = scheduleFireTime;
            bookingRequest.Status = BookingStatus.Scheduled;
            await _dbContext.SaveChangesAsync();

            var jobId = BackgroundJob.Schedule<BookTeeTimeJob>(
                job => job.ExecuteAsync(bookingRequestId),
                scheduleFireTime);

            bookingRequest.HangfireJobId = jobId;
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("ScheduleBookingJob completed for booking request {BookingRequestId}", bookingRequestId);
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

    private ReleaseSchedule? ParseReleaseSchedule(string? scheduleJson)
    {
        if (string.IsNullOrEmpty(scheduleJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ReleaseSchedule>(scheduleJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse release schedule JSON");
            return null;
        }
    }

    private DateTime CalculateScheduleFireTime(DateTime desiredDate, ReleaseSchedule schedule)
    {
        var releaseDate = desiredDate.AddDays(-schedule.DaysInAdvance).Date;

        // Parse "HH:mm" release time string
        var releaseDateTime = releaseDate;
        if (!string.IsNullOrEmpty(schedule.ReleaseTime) &&
            TimeOnly.TryParse(schedule.ReleaseTime, out var releaseTime))
        {
            releaseDateTime = releaseDate.Add(releaseTime.ToTimeSpan());
        }
        else
        {
            releaseDateTime = releaseDate.AddHours(schedule.ReleaseHour).AddMinutes(schedule.ReleaseMinute);
        }

        if (releaseDateTime <= DateTime.UtcNow)
        {
            _logger.LogWarning("Calculated release time {ReleaseTime} is in the past, scheduling for immediate execution", releaseDateTime);
            return DateTime.UtcNow.AddSeconds(5);
        }

        _logger.LogInformation("Booking scheduled to fire at {FireTime} (release window for {DesiredDate})", releaseDateTime, desiredDate);
        return releaseDateTime;
    }

    private class ReleaseSchedule
    {
        public int DaysInAdvance { get; set; } = 7;
        public string? ReleaseTime { get; set; }  // "HH:mm" — matches stored JSON format
        public int ReleaseHour { get; set; } = 8;   // fallback if ReleaseTime missing
        public int ReleaseMinute { get; set; } = 0;
    }
}
