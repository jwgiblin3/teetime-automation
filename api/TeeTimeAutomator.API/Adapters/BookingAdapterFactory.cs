using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using TeeTimeAutomator.API.Models.Enums;

namespace TeeTimeAutomator.API.Adapters;

/// <summary>
/// Factory for creating appropriate booking adapters based on platform
/// </summary>
public interface IBookingAdapterFactory
{
    /// <summary>
    /// Create a booking adapter for the specified platform
    /// </summary>
    /// <param name="platform">The platform to create an adapter for</param>
    /// <returns>An adapter instance implementing IBookingAdapter</returns>
    IBookingAdapter CreateAdapter(CoursePlatform platform);
}

/// <summary>
/// Implementation of booking adapter factory using dependency injection
/// </summary>
public class BookingAdapterFactory : IBookingAdapterFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingAdapterFactory> _logger;

    public BookingAdapterFactory(IServiceProvider serviceProvider, ILogger<BookingAdapterFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public IBookingAdapter CreateAdapter(CoursePlatform platform)
    {
        _logger.LogInformation("Creating booking adapter for platform: {Platform}", platform);

        IBookingAdapter adapter = platform switch
        {
            CoursePlatform.CpsGolf => _serviceProvider.GetRequiredService<CpsGolfAdapter>(),
            CoursePlatform.GolfNow => _serviceProvider.GetRequiredService<GolfNowAdapter>(),
            CoursePlatform.TeeSnap => _serviceProvider.GetRequiredService<TeeSnapAdapter>(),
            CoursePlatform.ForeUp => _serviceProvider.GetRequiredService<ForeUpAdapter>(),
            _ => throw new ArgumentException($"Unknown platform: {platform}", nameof(platform))
        };

        _logger.LogInformation("Successfully created {AdapterType} for platform {Platform}", adapter.GetType().Name, platform);
        return adapter;
    }
}
