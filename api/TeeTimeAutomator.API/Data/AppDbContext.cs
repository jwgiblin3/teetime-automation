using Microsoft.EntityFrameworkCore;
using TeeTimeAutomator.API.Models;

namespace TeeTimeAutomator.API.Data;

/// <summary>
/// Entity Framework Core database context for TeeTimeAutomator.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the AppDbContext.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Users DbSet.
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Gets or sets the Courses DbSet.
    /// </summary>
    public DbSet<Course> Courses { get; set; } = null!;

    /// <summary>
    /// Gets or sets the UserCourseCredentials DbSet.
    /// </summary>
    public DbSet<UserCourseCredential> UserCourseCredentials { get; set; } = null!;

    /// <summary>
    /// Gets or sets the BookingRequests DbSet.
    /// </summary>
    public DbSet<BookingRequest> BookingRequests { get; set; } = null!;

    /// <summary>
    /// Gets or sets the BookingResults DbSet.
    /// </summary>
    public DbSet<BookingResult> BookingResults { get; set; } = null!;

    /// <summary>
    /// Gets or sets the AuditLogs DbSet.
    /// </summary>
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    /// <summary>
    /// Configures the model for the database.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.GoogleOAuthId).HasMaxLength(255);

            // Create a unique index on Email
            entity.HasIndex(e => e.Email).IsUnique();

            // Create a unique index on GoogleOAuthId (allowing nulls)
            entity.HasIndex(e => e.GoogleOAuthId).IsUnique().HasFilter("[GoogleOAuthId] IS NOT NULL");

            // Configure relationships
            entity.HasMany(e => e.UserCourseCredentials)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BookingRequests)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure Course entity
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId);
            entity.Property(e => e.CourseName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.BookingUrl).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ReleaseScheduleJson).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Platform).IsRequired();

            // Create an index on CourseName
            entity.HasIndex(e => e.CourseName);

            // Create an index on IsActive
            entity.HasIndex(e => e.IsActive);

            // Configure relationships
            entity.HasMany(e => e.UserCourseCredentials)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.BookingRequests)
                .WithOne(e => e.Course)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure UserCourseCredential entity
        modelBuilder.Entity<UserCourseCredential>(entity =>
        {
            entity.HasKey(e => e.CredentialId);
            entity.Property(e => e.EncryptedEmail).IsRequired().HasMaxLength(500);
            entity.Property(e => e.EncryptedPassword).IsRequired().HasMaxLength(500);

            // Create a unique index on UserId + CourseId
            entity.HasIndex(e => new { e.UserId, e.CourseId }).IsUnique();

            // Configure foreign keys
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserCourseCredentials)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Course)
                .WithMany(e => e.UserCourseCredentials)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure BookingRequest entity
        modelBuilder.Entity<BookingRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.DesiredDate).IsRequired();
            entity.Property(e => e.PreferredTime).IsRequired();
            entity.Property(e => e.HangfireJobId).HasMaxLength(255);

            // Create indexes for common queries
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ScheduledFireTime);
            entity.HasIndex(e => e.CourseId);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(e => e.BookingRequests)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Course)
                .WithMany(e => e.BookingRequests)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.BookingResult)
                .WithOne(e => e.BookingRequest)
                .HasForeignKey<BookingResult>(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.BookingRequest)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BookingResult entity
        modelBuilder.Entity<BookingResult>(entity =>
        {
            entity.HasKey(e => e.ResultId);
            entity.Property(e => e.ConfirmationNumber).HasMaxLength(255);
            entity.Property(e => e.FailureReason).HasMaxLength(1000);

            // Create an index on RequestId
            entity.HasIndex(e => e.RequestId).IsUnique();

            // Create an index on IsSuccess
            entity.HasIndex(e => e.IsSuccess);

            // Configure relationship
            entity.HasOne(e => e.BookingRequest)
                .WithOne(e => e.BookingResult)
                .HasForeignKey<BookingResult>(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure AuditLog entity
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.EventType).IsRequired();
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);

            // Create indexes for common queries
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RequestId);
            entity.HasIndex(e => e.EventType);

            // Configure relationships
            entity.HasOne(e => e.User)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.BookingRequest)
                .WithMany(e => e.AuditLogs)
                .HasForeignKey(e => e.RequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
