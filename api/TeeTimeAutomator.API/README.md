# TeeTimeAutomator API

A comprehensive C# .NET 8 Web API for automated golf tee time booking. The system automatically books tee times at specific times based on course release schedules using Hangfire background job scheduling.

## Features

- **User Authentication**: Email/password and Google OAuth authentication with JWT tokens
- **Course Management**: Register golf courses with their booking platforms and release schedules
- **Credential Encryption**: Securely store user golf course credentials with AES-256 encryption
- **Automated Booking**: Schedule automatic tee time bookings using Hangfire background jobs
- **SMS Notifications**: Send booking confirmations and failure notifications via Twilio
- **Calendar Integration**: Generate iCalendar files and integrate with Google Calendar
- **Audit Logging**: Track all system events and user actions
- **Admin Dashboard**: Hangfire dashboard for monitoring scheduled jobs

## Technology Stack

- **.NET 8.0**: Latest LTS version of .NET framework
- **Entity Framework Core**: ORM for database operations
- **SQL Server**: Relational database
- **Hangfire**: Background job scheduling and processing
- **JWT**: Secure API authentication
- **BCrypt**: Password hashing
- **Twilio**: SMS notifications
- **AutoMapper**: Object mapping
- **Swagger/OpenAPI**: API documentation

## Project Structure

```
TeeTimeAutomator.API/
├── Controllers/           # API endpoints
├── Data/                  # Entity Framework configuration
├── Models/                # Domain entities
│   ├── DTOs/             # Data transfer objects
│   └── Enums/            # Enumerations
├── Services/              # Business logic layer
├── Program.cs            # Application startup and configuration
└── appsettings.json      # Configuration file
```

## Models

### Core Models

- **User**: System users with authentication
- **Course**: Golf courses with booking information
- **UserCourseCredential**: Encrypted credentials for golf course accounts
- **BookingRequest**: Tee time booking requests scheduled for automation
- **BookingResult**: Results of completed booking attempts
- **AuditLog**: System event tracking

### Enumerations

- **BookingStatus**: Pending, Scheduled, InProgress, Booked, Failed, Cancelled
- **CoursePlatform**: CpsGolf, GolfNow, TeeSnap, ForeUp, Other
- **AuditEventType**: User registration, login, booking events, etc.

## Services

### Authentication Service (`IAuthService`)
- User registration with email and password
- Email/password login
- Google OAuth integration
- JWT token generation

### Course Service (`ICourseService`)
- CRUD operations for golf courses
- Encrypted credential storage and retrieval
- User course association

### Booking Service (`IBookingService`)
- Create and manage booking requests
- Calculate scheduled fire times based on course release schedules
- Schedule Hangfire background jobs
- Process bookings automatically
- Track booking status and results

### SMS Service (`ISmsService`)
- Send booking confirmations
- Send failure notifications
- Send security alerts

### Calendar Service (`ICalendarService`)
- Generate iCalendar (.ics) files
- Google Calendar integration stub

### Encryption Service (`IEncryptionService`)
- AES-256 encryption/decryption
- Secure credential storage

### Audit Service (`IAuditService`)
- Log all system events
- Track user actions

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login with credentials
- `POST /api/auth/google` - Login with Google OAuth
- `GET /api/auth/me` - Get current user info

### Courses
- `GET /api/courses` - List all courses
- `GET /api/courses/{id}` - Get course details
- `POST /api/courses` - Create course (admin)
- `PUT /api/courses/{id}` - Update course (admin)
- `DELETE /api/courses/{id}` - Delete course (admin)
- `POST /api/courses/credentials` - Store course credentials
- `GET /api/courses/me/courses` - Get user's courses with credentials
- `DELETE /api/courses/credentials/{courseId}` - Delete stored credentials

### Bookings
- `POST /api/bookings` - Create booking request
- `GET /api/bookings/{id}` - Get booking details
- `GET /api/bookings/me/requests` - Get user's bookings
- `GET /api/bookings/status/{status}` - Get bookings by status (admin)
- `GET /api/bookings/{id}/status` - Get booking status
- `POST /api/bookings/{id}/cancel` - Cancel booking

## Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=TeeTimeAutomatorDb;..."
  },
  "Jwt": {
    "Secret": "your-super-secret-jwt-key-minimum-32-characters",
    "Issuer": "TeeTimeAutomator",
    "Audience": "TeeTimeAutomatorClient",
    "ExpiryMinutes": 1440
  },
  "Google": {
    "ClientId": "your-google-oauth-client-id.apps.googleusercontent.com",
    "ClientSecret": "your-google-oauth-client-secret"
  },
  "Twilio": {
    "AccountSid": "your-twilio-account-sid",
    "AuthToken": "your-twilio-auth-token",
    "FromNumber": "+1234567890"
  },
  "Encryption": {
    "Key": "0123456789ABCDEF0123456789ABCDEF",
    "IV": "0123456789ABCDEF"
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:4200", "https://yourdomain.com"]
  }
}
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK or later
- SQL Server 2016 or later (LocalDB supported)
- Twilio account (optional, for SMS notifications)
- Google OAuth credentials (optional, for Google login)

### Installation

1. **Clone the repository**
```bash
git clone <repository-url>
cd TeeTimeAutomator.API
```

2. **Install dependencies**
```bash
dotnet restore
```

3. **Configure database connection** in `appsettings.json`

4. **Apply database migrations**
```bash
dotnet ef database update
```

5. **Run the application**
```bash
dotnet run
```

The API will be available at `https://localhost:5001` and Swagger documentation at `https://localhost:5001/swagger`.

## Database Migrations

The application automatically applies migrations on startup. To manually manage migrations:

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply pending migrations
dotnet ef database update

# Revert to previous migration
dotnet ef database update PreviousMigrationName
```

## Hangfire Dashboard

Monitor scheduled and executed jobs at `/hangfire` (admin users only).

## Security Considerations

- All passwords are hashed with BCrypt
- Course credentials are encrypted with AES-256
- JWT tokens are signed with HS256
- CORS is configured for specific origins
- Authentication is required for most endpoints
- Admin-only endpoints require authorization claims

## Error Handling

The API returns appropriate HTTP status codes:
- `200 OK` - Successful request
- `201 Created` - Resource created
- `400 Bad Request` - Invalid input
- `401 Unauthorized` - Missing or invalid authentication
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource already exists
- `500 Internal Server Error` - Server error

All error responses include a `ProblemDetails` object with error information.

## Logging

The application uses Serilog for structured logging. Logs are written to the console and can be configured to write to other sinks (files, cloud services, etc.).

## Performance Considerations

- Database queries use indexes on frequently filtered columns
- Async/await is used throughout for non-blocking operations
- Hangfire uses SQL Server for persistent job storage
- AutoMapper profiles are configured for efficient mapping

## Testing

Example requests can be found in the Swagger documentation. The API is ready for integration with frontend applications and automated testing frameworks.

## License

MIT License

## Support

For issues, feature requests, or questions, please create an issue in the repository.
