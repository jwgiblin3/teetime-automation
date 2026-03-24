namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Represents a tee time slot available for booking
/// </summary>
public class TeeTimeSlot
{
    /// <summary>
    /// Unique identifier for the slot
    /// </summary>
    public string SlotId { get; set; } = string.Empty;

    /// <summary>
    /// Date and time of the tee time
    /// </summary>
    public DateTime DateTime { get; set; }

    /// <summary>
    /// Number of players that can still be booked for this slot
    /// </summary>
    public int AvailablePlayers { get; set; }

    /// <summary>
    /// Price per player (optional)
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Course name
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Hole count (9 or 18)
    /// </summary>
    public int? Holes { get; set; }
}

/// <summary>
/// Result of a booking attempt
/// </summary>
public class BookingAdapterResult
{
    /// <summary>
    /// Whether the booking was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Confirmation number from the booking site
    /// </summary>
    public string? ConfirmationNumber { get; set; }

    /// <summary>
    /// Error message if booking failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Booked tee time
    /// </summary>
    public DateTime? BookedTime { get; set; }

    /// <summary>
    /// Total price paid
    /// </summary>
    public decimal? TotalPrice { get; set; }
}

/// <summary>
/// Interface for golf course booking site adapters
/// </summary>
public interface IBookingAdapter
{
    /// <summary>
    /// Login to the booking site
    /// </summary>
    /// <param name="url">Base URL of the booking site</param>
    /// <param name="email">User email</param>
    /// <param name="password">User password</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if login successful, false otherwise</returns>
    Task<bool> LoginAsync(string url, string email, string password, CancellationToken ct = default);

    /// <summary>
    /// Search for available tee time slots
    /// </summary>
    /// <param name="date">Date to search for</param>
    /// <param name="preferredTime">Preferred tee time (HH:mm)</param>
    /// <param name="windowMinutes">Search window in minutes around preferred time</param>
    /// <param name="players">Number of players</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of available slots matching criteria</returns>
    Task<List<TeeTimeSlot>> SearchAvailableSlotsAsync(
        DateTime date,
        TimeSpan preferredTime,
        int windowMinutes,
        int players,
        CancellationToken ct = default);

    /// <summary>
    /// Book a specific tee time slot
    /// </summary>
    /// <param name="slot">The slot to book</param>
    /// <param name="players">Number of players for this booking</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Booking result with confirmation number if successful</returns>
    Task<BookingAdapterResult> BookSlotAsync(TeeTimeSlot slot, int players, CancellationToken ct = default);

    /// <summary>
    /// Logout from the booking site and cleanup resources
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    Task LogoutAsync(CancellationToken ct = default);
}
