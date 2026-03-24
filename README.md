# TeeTimeAutomator

Automated golf tee time booking system that monitors multiple golf platforms and securely reserves tee times based on user preferences using browser automation with Playwright.

## Overview

TeeTimeAutomator eliminates the hassle of manually checking golf course websites to find available tee times. The system monitors multiple golf booking platforms, and when a tee time matching your preferences becomes available, it automatically books it for you. Built with .NET 8, Angular 17, and SQL Server, the application integrates with four major golf booking platforms.

### System Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    User Requests                             │
│  (via Web UI or Mobile Browser)                              │
└──────────────────────┬──────────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        ▼                              ▼
┌──────────────────────┐    ┌─────────────────────┐
│   Angular 17 Web     │    │  REST API (.NET 8)  │
│   Responsive SPA     │◄───►  JWT Authentication │
└──────────────────────┘    │  Swagger Docs       │
                            └─────────────────────┘
                                     │
                ┌────────────┬────────┴────────┬────────────┐
                ▼            ▼                 ▼            ▼
        ┌──────────────┬──────────────┬──────────────┬──────────────┐
        │  Playwright  │  Hangfire    │  SQL Server  │  Audit Logs  │
        │  Automation  │  Job Queues  │  Database    │  Compliance  │
        └──────────────┴──────────────┴──────────────┴──────────────┘
                │
        ┌───────┴────────┬────────────┬────────────┬────────────┐
        ▼                ▼            ▼            ▼            ▼
    ┌────────┐      ┌────────┐  ┌────────┐  ┌────────┐  ┌────────┐
    │ CPS    │      │GolfNow │  │TeeSnap │  │ ForeUp │  │ Others │
    │ Golf   │      │        │  │        │  │        │  │        │
    └────────┘      └────────┘  └────────┘  └────────┘  └────────┘
```

## Tech Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Backend** | .NET | 8.0 | API Framework |
| | Entity Framework Core | 8.0+ | ORM & Database |
| | Hangfire | 1.7+ | Background Jobs |
| | Playwright .NET | 1.40+ | Browser Automation |
| | JWT Bearer | - | Authentication |
| | Swagger | 6.0+ | API Documentation |
| **Database** | SQL Server | 2019+ | Data Storage |
| **Frontend** | Angular | 17+ | SPA Framework |
| | TypeScript | 5.2+ | Programming Language |
| | Bootstrap | 5+ | UI Components |
| | RxJS | 7.8+ | Reactive Programming |
| **DevOps** | Docker | 24+ | Containerization |
| | Docker Compose | 2.0+ | Local Development |

## Quick Start (Docker)

### Prerequisites
- Docker and Docker Compose installed
- Git

### Steps

```bash
# 1. Clone the repository
git clone https://github.com/yourusername/TeeTimeAutomator.git
cd TeeTimeAutomator

# 2. Start all services
docker-compose up -d

# 3. Access the application
# Web UI:   http://localhost:4200
# API Docs: http://localhost:5000/swagger
# DB:       localhost:1433 (SQL Server)
```

The system is fully operational within 30-60 seconds. The database initializes automatically with schema and seed data.

## Manual Setup

### Prerequisites
- .NET 8 SDK
- Node.js 20+
- SQL Server 2019+ (local or remote)
- Visual Studio 2022, VS Code, or JetBrains Rider

### Backend Setup

```bash
# Navigate to API project
cd api/TeeTimeAutomator.API

# Restore dependencies
dotnet restore

# Update database (creates schema and seed data)
dotnet ef database update

# Build the solution
dotnet build

# Run the API (starts on http://localhost:5000)
dotnet run
```

### Frontend Setup

```bash
# Navigate to web project
cd web

# Install dependencies
npm install

# Start development server (opens on http://localhost:4200)
ng serve --open

# Or build for production
npm run build
```

### Database Setup

#### Option 1: Using Migration Scripts

```bash
# Using SQL Server Management Studio or sqlcmd:
sqlcmd -S localhost -U sa -P TeeTime_Dev2024! -i database/001_initial_schema.sql
sqlcmd -S localhost -U sa -P TeeTime_Dev2024! -i database/002_hangfire_tables.sql
```

#### Option 2: Using Entity Framework

```bash
cd api/TeeTimeAutomator.API
dotnet ef database update
```

## Environment Variables

### API Configuration

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | - | `Server=localhost;Database=TeeTimeAutomator;User Id=sa;Password=YourPassword;TrustServerCertificate=True` |
| `JWT__Secret` | JWT signing key (min 32 chars) | - | `YourVerySecureSecretKeyWith32OrMoreCharacters` |
| `JWT__Issuer` | JWT issuer claim | `TeeTimeAutomator` | - |
| `JWT__Audience` | JWT audience claim | `TeeTimeAutomator` | - |
| `ASPNETCORE_ENVIRONMENT` | Environment mode | `Production` | `Development` / `Production` |
| `ASPNETCORE_URLS` | API listen address | `http://+:5000` | `http://+:8080` |
| `Hangfire__DashboardUrl` | Hangfire dashboard path | `/hangfire` | - |
| `Playwright__HeadlessMode` | Run Playwright headless | `true` | - |
| `Playwright__Timeout` | Browser timeout (ms) | `30000` | - |

### Frontend Configuration

| Variable | Description | Default | Example |
|----------|-------------|---------|---------|
| `NG_APP_API_BASE_URL` | API endpoint URL | `/api` | `http://localhost:5000/api` |
| `NG_APP_ENVIRONMENT` | Environment | `production` | `development` / `production` |

## Supported Golf Platforms

| Platform | Status | Login Method | Notes |
|----------|--------|--------------|-------|
| **CPS Golf** | Implemented | Username/Password | Paramus, Pinehurst, Renaissance courses |
| **GolfNow** | Implemented | Email/Password | 18,000+ courses worldwide |
| **TeeSnap** | Implemented | Email/Password | ResortTrack integration |
| **ForeUp** | Implemented | Email/Password | Club management standard |
| **Grint** | Planned | OAuth | Mobile-first booking |
| **Eventbrite Golf** | Planned | OAuth | Corporate events |

## Features

### Current (Phase 1)
- Multi-platform golf course support
- Secure credential management (encrypted storage)
- Booking automation with Playwright
- Background job processing with Hangfire
- User authentication with JWT
- Audit logging for compliance
- Responsive web interface

### In Development (Phase 2)
- SMS/Email notifications
- Multi-golfer group bookings
- Pricing comparison across platforms
- Tee time recommendations
- Mobile app (iOS/Android)

### Planned (Phase 3)
- Machine learning preference learning
- Social golf group management
- Gamification (achievements, leaderboards)
- Payment processing integration
- Waitlist management

## API Endpoints

### Authentication
```
POST   /api/auth/register          Register new user
POST   /api/auth/login             Login with credentials
POST   /api/auth/refresh           Refresh JWT token
POST   /api/auth/logout            Logout (invalidate token)
```

### User Management
```
GET    /api/users/profile          Get current user profile
PUT    /api/users/profile          Update profile
GET    /api/users/{id}             Get user by ID (admin)
DELETE /api/users/{id}             Delete user (admin)
```

### Courses
```
GET    /api/courses                List all courses
GET    /api/courses/{id}           Get course details
POST   /api/courses                Create new course (admin)
PUT    /api/courses/{id}           Update course (admin)
DELETE /api/courses/{id}           Delete course (admin)
```

### Credentials
```
GET    /api/credentials            Get user's credentials
POST   /api/credentials            Add platform credential
PUT    /api/credentials/{id}       Update credential
DELETE /api/credentials/{id}       Delete credential
```

### Booking Requests
```
GET    /api/bookings               List user's booking requests
POST   /api/bookings               Create booking request
PUT    /api/bookings/{id}          Update booking request
DELETE /api/bookings/{id}          Delete booking request
GET    /api/bookings/{id}/results  View booking results
```

### Admin
```
GET    /api/admin/audit-logs       View audit logs
GET    /api/admin/jobs             View background jobs
GET    /api/admin/stats            System statistics
```

## How Automated Booking Works

### Step-by-Step Process

1. **User Configuration**
   - User logs in and adds their golf course credentials
   - Creates a booking request with preferences (dates, times, players)

2. **Scheduling**
   - System adds the request to Hangfire queue
   - Scheduler triggers booking job based on criteria

3. **Availability Check**
   - Playwright launches browser (headless)
   - Navigates to golf platform using user's credentials
   - Searches for matching tee times

4. **Booking Execution**
   - If tee time matches criteria, system books it
   - Captures confirmation number
   - Records booking result in database

5. **Notification**
   - User receives notification (email/SMS)
   - Booking details visible in dashboard

### Booking Preferences

Users can customize:
- **Dates**: Specific dates or date range
- **Times**: Preferred start times
- **Course Holes**: 9-hole front/back, 18-hole, or any
- **Players**: Minimum and maximum golfers
- **Days**: Weekday only, weekend only, or any
- **Booking Window**: How many days ahead to book

### Example Timeline

```
User creates booking request
         ↓
Daily scheduler triggers at 6:00 AM
         ↓
Availability check begins (7:00 AM)
         ↓
Tee times released on platform
         ↓
System detects match instantly
         ↓
Automated booking executed
         ↓
Confirmation number captured
         ↓
User notified (email/SMS)
         ↓
Golfers receive booking confirmation
```

## Database Schema

### Users
- UserId (PK, GUID)
- Email (unique)
- PasswordHash
- FirstName, LastName
- IsAdmin, IsActive
- CreatedAt, UpdatedAt

### Courses
- CourseId (PK, GUID)
- Name, Location
- Platform (CPS Golf, GolfNow, TeeSnap, ForeUp)
- PlatformCourseId
- Holes, Par, Slope, Rating
- IsActive
- CreatedAt, UpdatedAt

### UserCourseCredentials
- CredentialId (PK, GUID)
- UserId (FK)
- CourseId (FK)
- PlatformUsername, PlatformPassword (encrypted)
- HandicapIndex
- IsActive
- CreatedAt, UpdatedAt
- Unique constraint on (UserId, CourseId)

### BookingRequests
- RequestId (PK, GUID)
- UserId (FK), CourseId (FK)
- PreferredPlayDates (JSON)
- PreferredStartTimes (JSON)
- NumberOfPlayers, MinimumGolfers, MaximumGolfers
- BookingWindowStart, BookingWindowEnd
- CourseHolePreference
- WeekdayOnly, WeekendOnly
- IsActive
- CreatedAt, UpdatedAt

### BookingResults
- ResultId (PK, GUID)
- RequestId (FK), UserId (FK), CourseId (FK)
- AttemptedDate, AttemptedStartTime, AttemptedNumberOfPlayers
- Status (Success, Failed, NoAvailability, Error)
- ConfirmationNumber, PlatformBookingId
- ErrorMessage, BookingPrice
- AttemptedAt, CompletedAt

### AuditLogs
- LogId (PK, GUID)
- UserId (FK, nullable)
- Action, EntityType, EntityId
- OldValues, NewValues (JSON)
- IpAddress, UserAgent
- CreatedAt

## Development

### Project Structure

```
TeeTimeAutomator/
├── api/
│   └── TeeTimeAutomator.API/
│       ├── Controllers/
│       ├── Services/
│       ├── Data/
│       │   ├── Migrations/
│       │   └── TeeTimeContext.cs
│       ├── Models/
│       ├── DTOs/
│       ├── Utilities/
│       └── appsettings.json
├── web/
│   ├── src/
│   │   ├── app/
│   │   │   ├── auth/
│   │   │   ├── dashboard/
│   │   │   ├── bookings/
│   │   │   ├── courses/
│   │   │   └── shared/
│   │   ├── assets/
│   │   └── styles/
│   ├── angular.json
│   └── package.json
├── database/
│   ├── 001_initial_schema.sql
│   └── 002_hangfire_tables.sql
└── docker-compose.yml
```

### Running Tests

```bash
# Backend unit tests
cd api/TeeTimeAutomator.API
dotnet test

# Frontend unit tests
cd web
npm run test

# E2E tests
npm run e2e
```

### Code Style & Guidelines

- **Backend**: Follow Microsoft C# Coding Conventions
- **Frontend**: Follow Angular style guide and ESLint config
- **Database**: Use UPPER_CASE for tables, lowercase for columns
- **Commits**: Use conventional commits (feat:, fix:, docs:, etc.)

## Development Roadmap

### Phase 1 (Complete)
- Core automation engine with Playwright
- Multi-platform support (4 platforms)
- User authentication and authorization
- Booking request management
- Database schema and migrations
- REST API with Swagger documentation
- Angular SPA with responsive design
- Docker containerization

### Phase 2 (In Progress)
- Email and SMS notifications
- Advanced group booking features
- Pricing comparison and analytics
- Improved UI/UX with booking history
- Mobile app (React Native)
- Admin dashboard and monitoring

### Phase 3 (Planned)
- Machine learning for preference prediction
- Social golf group management
- Gamification and achievements
- Payment processing (Stripe/PayPal)
- Waitlist management system
- Calendar integration (Google/Outlook)
- White-label licensing options

## Security Considerations

- **Credential Storage**: Platform credentials encrypted at rest using AES-256
- **Authentication**: JWT tokens with 24-hour expiration
- **Authorization**: Role-based access control (user, admin)
- **API Security**: HTTPS required, CORS configured
- **Database**: SQL Server with parameterized queries (Entity Framework)
- **Audit Trail**: All actions logged for compliance
- **Input Validation**: Server-side validation on all endpoints
- **Rate Limiting**: Implemented on public endpoints

## Troubleshooting

### Common Issues

**API fails to connect to database**
```
Error: "Connection timeout"
Solution: Ensure SQL Server is running, credentials are correct,
          network connectivity exists
```

**Playwright browser installation fails**
```
Error: "Could not find chromium"
Solution: Run: dotnet tool install --global Microsoft.Playwright.CLI
          Then: playwright install chromium
```

**CORS errors in Angular**
```
Error: "Access to XMLHttpRequest blocked"
Solution: Verify API_URL in environment files matches running API
```

**Authentication token expired**
```
Error: "401 Unauthorized"
Solution: Click refresh token or logout/login
```

## Contributing

We welcome contributions! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit changes (`git commit -m 'Add amazing feature'`)
4. Push to branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Guidelines
- Write clear commit messages
- Add tests for new features
- Update documentation
- Follow code style guidelines
- Reference issues in PR description

## License

This project is licensed under the MIT License - see LICENSE file for details.

## Support

- Documentation: [Wiki](https://github.com/yourusername/TeeTimeAutomator/wiki)
- Issues: [GitHub Issues](https://github.com/yourusername/TeeTimeAutomator/issues)
- Discussions: [GitHub Discussions](https://github.com/yourusername/TeeTimeAutomator/discussions)

## Disclaimer

TeeTimeAutomator automates bookings on third-party golf platforms. Users are responsible for:
- Complying with each platform's terms of service
- Maintaining accurate credential information
- Not using automation for unauthorized purposes
- Respecting course booking limits and policies

The developers are not responsible for bookings made or issues arising from platform changes.

## Changelog

### v1.0.0 (Current)
- Initial release
- Four platform support (CPS Golf, GolfNow, TeeSnap, ForeUp)
- User authentication and management
- Automated booking system
- Audit logging

---

**Built with love for golfers who just want to play golf**
