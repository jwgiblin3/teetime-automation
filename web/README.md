# TeeTime Automator - Angular 17 Frontend

A complete, production-ready Angular 17 frontend for the TeeTime Automator golf tee time booking automation application.

## Project Structure

```
src/
├── app/
│   ├── guards/
│   │   ├── auth.guard.ts                 # Authentication guard
│   │   └── admin.guard.ts                # Admin authorization guard
│   ├── interceptors/
│   │   ├── auth.interceptor.ts           # JWT token injection
│   │   └── error.interceptor.ts          # Global error handling
│   ├── models/
│   │   ├── auth.models.ts                # Auth data models
│   │   ├── booking.models.ts             # Booking data models
│   │   ├── course.models.ts              # Course data models
│   │   └── admin.models.ts               # Admin data models
│   ├── services/
│   │   ├── api.service.ts                # Base API service
│   │   ├── auth.service.ts               # Authentication service
│   │   ├── booking.service.ts            # Booking service
│   │   ├── course.service.ts             # Course service
│   │   └── admin.service.ts              # Admin service
│   ├── layout/
│   │   └── navbar/                       # Navigation bar component
│   ├── pages/
│   │   ├── auth/
│   │   │   ├── login/                    # Login page
│   │   │   └── register/                 # Registration page
│   │   ├── dashboard/                    # User dashboard
│   │   ├── bookings/
│   │   │   ├── booking-list/             # Bookings list
│   │   │   ├── booking-create/           # Create booking with stepper
│   │   │   └── booking-detail/           # Booking details
│   │   ├── courses/
│   │   │   ├── course-list/              # Courses list
│   │   │   └── course-form/              # Create/edit course with wizard
│   │   ├── profile/                      # User profile
│   │   └── admin/
│   │       ├── admin-dashboard/          # Admin overview
│   │       ├── admin-users/              # User management
│   │       └── admin-bookings/           # All bookings management
│   ├── shared/
│   │   ├── status-chip/                  # Status badge component
│   │   └── confirm-dialog/               # Confirmation dialog
│   ├── app.component.ts                  # Root component
│   ├── app.config.ts                     # App configuration
│   └── app.routes.ts                     # Route definitions
├── environments/
│   ├── environment.ts                    # Development config
│   └── environment.prod.ts               # Production config
├── styles.scss                           # Global styles (dark golf theme)
├── index.html                            # HTML entry point
└── main.ts                               # Bootstrap file
```

## Features

### Authentication
- **Login**: Email/password authentication with JWT tokens
- **Register**: New user registration with validation
- **Google Sign-In**: OAuth 2.0 integration ready (configure with your credentials)
- **JWT Management**: Automatic token injection via interceptor
- **Token Validation**: Automatic logout on expired tokens

### User Dashboard
- Overview statistics (active bookings, success rate, configured courses)
- Recent bookings preview
- Quick navigation to common actions
- Responsive card-based layout

### Booking Management
- **Create Bookings**: Multi-step wizard with:
  - Course selection
  - Date/time selection with Material date picker
  - Time window flexibility slider (0-120 minutes)
  - Number of players selector (1-4)
  - Review and confirmation
- **View Bookings**: List with status filtering
- **Booking Details**: Full details with timeline and results
- **Cancel Bookings**: Cancel pending bookings
- **Retry Bookings**: Retry failed booking attempts

### Course Management
- **Add Courses**: Multi-step form with:
  - Course name and booking URL
  - Platform selection (CPS Golf, GolfNow, TeeSnap, ForeUp, Other)
  - Release schedule configuration
  - Secure credential storage
- **View Courses**: Card-based course display
- **Edit Courses**: Update course settings
- **Delete Courses**: Remove courses
- **Credential Status**: Visual indicator for saved credentials

### User Profile
- **Personal Information**: Edit name and phone
- **Security**: Change password with validation
- **Connected Courses**: View all configured courses

### Admin Dashboard
- **System Statistics**: Users, bookings, success rates, active polls
- **Performance Metrics**: Average booking time, user engagement
- **Recent Activity**: Audit log of system actions
- **User Management**: View, enable/disable users
- **Booking Management**: View all user bookings
- **Pagination**: Efficient data loading with pagination

### UI/UX
- **Dark Golf Theme**: Professional dark green color scheme
- **Material Design**: Angular Material components throughout
- **Responsive Layout**: Mobile-friendly design
- **Loading States**: Spinners and disabled states
- **Error Handling**: Global error interceptor with snackbars
- **Forms**: Reactive forms with comprehensive validation
- **Navigation**: Smooth routing with guards

## Technologies

- **Angular 17**: Latest Angular framework
- **Angular Material**: UI component library
- **RxJS**: Reactive programming
- **TypeScript**: Strongly-typed JavaScript
- **SCSS**: Advanced styling with variables and mixins
- **Reactive Forms**: Form validation and state management

## Configuration

### Development

1. **Install Dependencies**
```bash
npm install
```

2. **Update Environment**
Edit `src/environments/environment.ts`:
```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',  // Your backend URL
  googleClientId: 'YOUR_CLIENT_ID'  // Your Google OAuth Client ID
};
```

3. **Start Dev Server**
```bash
ng serve
```

Navigate to `http://localhost:4200/`

### Production

1. **Build**
```bash
ng build
```

2. **Update Environment**
Edit `src/environments/environment.prod.ts`:
```typescript
export const environment = {
  production: true,
  apiUrl: '/api',  // Relative API URL for deployment
  googleClientId: 'YOUR_PROD_CLIENT_ID'
};
```

3. **Deploy**
The `dist/teetime-automator-web` folder contains production files ready for deployment.

## Key Components

### Authentication Service
- JWT token management
- User session tracking
- Role-based access control
- Automatic token decoding

### API Service
- Base HTTP service with error handling
- Parameter building
- URL construction
- Authorization header injection

### Guards
- **AuthGuard**: Protects authenticated routes
- **AdminGuard**: Protects admin-only routes

### Interceptors
- **AuthInterceptor**: Adds JWT token to all requests
- **ErrorInterceptor**: Handles 401, 403, 500 errors globally

## Styling

### Color Palette
- **Primary Green**: `#2e7d32` (golf theme)
- **Light Green**: `#43a047`
- **Dark Green**: `#0d3817`
- **Background**: `#121212` (dark mode)
- **Card Background**: `#1e1e1e`

### Global Utilities
- Flexbox utilities (`.flex-center`, `.flex-between`, `.flex-column`)
- Spacing utilities (`.gap-1`, `.gap-2`, `.mt-1`, `.mb-2`, etc.)
- Text utilities (`.text-center`, `.text-muted`, `.text-success`)
- Layout utilities (`.grid-container`, `.form-row`)

## API Integration

The frontend expects the following API endpoints:

```
POST   /auth/login
POST   /auth/register
GET    /courses
POST   /courses
GET    /courses/:id
PUT    /courses/:id
DELETE /courses/:id
POST   /courses/:id/credentials
GET    /bookings
POST   /bookings
GET    /bookings/:id
POST   /bookings/:id/cancel
POST   /bookings/:id/retry
GET    /admin/users
GET    /admin/bookings
GET    /admin/stats
GET    /admin/activity
GET    /admin/logs
POST   /admin/users/:id/disable
POST   /admin/users/:id/enable
GET    /admin/export/bookings
GET    /admin/export/users
```

## Error Handling

The error interceptor automatically handles:
- **401 Unauthorized**: Redirects to login
- **403 Forbidden**: Shows permission error
- **404 Not Found**: Shows resource not found error
- **500 Server Error**: Shows server error message
- **Network Error**: Shows connection error
- **Custom Error Messages**: From API response

## Best Practices Implemented

- **Standalone Components**: All components use Angular 17 standalone pattern
- **Reactive Forms**: FormBuilder with validators
- **Strong Typing**: Full TypeScript strict mode
- **Lazy Loading**: Lazy-loaded routes for better performance
- **Change Detection**: OnPush strategy ready
- **Accessibility**: Semantic HTML with ARIA labels
- **Security**: JWT tokens, secure credential handling
- **Code Organization**: Clear separation of concerns
- **Reusable Components**: Shared components for common UI patterns
- **Error Handling**: Global error interceptor
- **Loading States**: User feedback for async operations

## Development Commands

```bash
# Start dev server
ng serve

# Build for production
ng build

# Run tests
ng test

# Run linting
ng lint

# Create new component
ng generate component pages/my-feature/my-component
```

## Browser Support

- Chrome (latest)
- Firefox (latest)
- Safari (latest)
- Edge (latest)

## License

MIT License - See LICENSE file for details

## Support

For issues or questions, please contact support@teetimeautomator.com
