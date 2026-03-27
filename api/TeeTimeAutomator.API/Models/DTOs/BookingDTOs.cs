using Newtonsoft.Json;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Models.DTOs;

/// <summary>
/// DTO for creating a booking request. Field names match the Angular frontend payload.
/// </summary>
public class CreateBookingRequest
{
    /// <summary>
    /// The ID of the golf course to book.
    /// </summary>
    [JsonProperty("courseId")]
    public int CourseId { get; set; }

    /// <summary>
    /// The desired date for the tee time.
    /// </summary>
    [JsonProperty("requestedDate")]
    public DateTime DesiredDate { get; set; }

    /// <summary>
    /// The preferred time as "HH:mm" string (e.g. "14:30").
    /// Stored as string because Newtonsoft.Json cannot deserialize TimeOnly.
    /// </summary>
    [JsonProperty("preferredTime")]
    public string PreferredTimeString { get; set; } = "00:00";

    /// <summary>
    /// Acceptable window in minutes around preferred time (default: 30).
    /// </summary>
    [JsonProperty("timeWindowMinutes")]
    public int TimeWindowMinutes { get; set; } = 30;

    /// <summary>
    /// Number of players for this tee time.
    /// </summary>
    [JsonProperty("numberOfPlayers")]
    public int NumberOfPlayers { get; set; }

    /// <summary>
    /// Parses PreferredTimeString into a TimeOnly value.
    /// </summary>
    public TimeOnly GetPreferredTime() =>
        TimeOnly.TryParseExact(PreferredTimeString, "HH:mm", out var t) ? t : TimeOnly.MinValue;
}

/// <summary>
/// DTO for booking request information. Field names match the Angular BookingRequest interface.
/// </summary>
public class BookingRequestDto
{
    // ── Internal backing fields (used in C# but not serialized) ──────────
    [JsonIgnore] public int RequestId { get; set; }
    [JsonIgnore] public int UserId { get; set; }
    [JsonIgnore] public int CourseId { get; set; }
    [JsonIgnore] public TimeOnly PreferredTime { get; set; }
    [JsonIgnore] public BookingStatus Status { get; set; }

    // ── Frontend-compatible serialized properties ─────────────────────────
    [JsonProperty("id")]
    public string Id => RequestId.ToString();

    [JsonProperty("userId")]
    public string UserIdString => UserId.ToString();

    [JsonProperty("courseId")]
    public string CourseIdString => CourseId.ToString();

    [JsonProperty("courseName")]
    public string CourseName { get; set; } = string.Empty;

    [JsonProperty("requestedDate")]
    public DateTime DesiredDate { get; set; }

    /// <summary>Preferred time serialized as "HH:mm" string for the frontend.</summary>
    [JsonProperty("preferredTime")]
    public string PreferredTimeFormatted => PreferredTime.ToString("HH:mm");

    [JsonProperty("timeWindowMinutes")]
    public int TimeWindowMinutes { get; set; }

    [JsonProperty("numberOfPlayers")]
    public int NumberOfPlayers { get; set; }

    /// <summary>Status serialized as lowercase string matching Angular BookingStatus enum.</summary>
    [JsonProperty("status")]
    public string StatusString => Status switch
    {
        BookingStatus.Pending    => "pending",
        BookingStatus.Scheduled  => "scheduled",
        BookingStatus.InProgress => "in-progress",
        BookingStatus.Booked     => "booked",
        BookingStatus.Failed     => "failed",
        BookingStatus.Cancelled  => "cancelled",
        _                        => "pending"
    };

    [JsonProperty("scheduledFireTime")]
    public DateTime? ScheduledFireTime { get; set; }

    [JsonProperty("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonProperty("bookingResult")]
    public BookingResultDto? BookingResult { get; set; }
}

/// <summary>
/// DTO for booking result information.
/// </summary>
public class BookingResultDto
{
    /// <summary>
    /// The booking result's unique identifier.
    /// </summary>
    public int ResultId { get; set; }

    /// <summary>
    /// The actual booked time.
    /// </summary>
    public DateTime? BookedTime { get; set; }

    /// <summary>
    /// Confirmation number from the booking system.
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Number of booking attempts made.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Timestamp of the last booking attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Reason for failure (if applicable).
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Whether the booking was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// When the result was created/updated.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for booking status information.
/// </summary>
public class BookingStatusDto
{
    /// <summary>
    /// The booking request's unique identifier.
    /// </summary>
    public int RequestId { get; set; }

    /// <summary>
    /// Current status of the booking request.
    /// </summary>
    public BookingStatus Status { get; set; }

    /// <summary>
    /// When the automatic booking is scheduled to fire.
    /// </summary>
    public DateTime? ScheduledFireTime { get; set; }

    /// <summary>
    /// The booking result (if available).
    /// </summary>
    public BookingResultDto? BookingResult { get; set; }

    /// <summary>
    /// Status message providing more context.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
