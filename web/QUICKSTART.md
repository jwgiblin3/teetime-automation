# TeeTime Automator - Quick Start Guide

## Prerequisites

- Node.js 18+ and npm
- Angular CLI 17: `npm install -g @angular/cli`
- A code editor (VS Code recommended)
- Running backend API (see API Configuration below)

## Installation (2 minutes)

```bash
# 1. Navigate to project directory
cd /path/to/TeeTimeAutomator/web

# 2. Install dependencies
npm install

# 3. Configure backend API (see below)

# 4. Start development server
ng serve

# 5. Open http://localhost:4200 in your browser
```

## API Configuration

### Development Environment

Edit `src/environments/environment.ts`:

```typescript
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5000',  // Change to your backend URL
  googleClientId: 'YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com'
};
```

### Production Environment

Edit `src/environments/environment.prod.ts`:

```typescript
export const environment = {
  production: true,
  apiUrl: 'https://api.yourdomain.com',  // Your production API
  googleClientId: 'YOUR_PROD_CLIENT_ID.apps.googleusercontent.com'
};
```

## First Run

1. **Development Server**: `ng serve`
2. **Navigate to**: http://localhost:4200
3. **See Login Page**: Default route redirects to /login
4. **Test Account**: Create one by clicking "Create one now" or use test credentials

## Project Structure

```
src/
├── app/                    # Application code
│   ├── pages/             # Page components (auth, dashboard, bookings, etc.)
│   ├── services/          # API and business logic services
│   ├── models/            # TypeScript interfaces and types
│   ├── guards/            # Route guards (auth, admin)
│   ├── interceptors/      # HTTP interceptors
│   ├── layout/            # Layout components (navbar)
│   └── shared/            # Reusable components
├── environments/          # Environment configurations
├── styles.scss           # Global styles
└── index.html           # HTML entry point
```

## Available Commands

```bash
# Development server
ng serve
# Navigate to http://localhost:4200

# Build for production
ng build
# Output in dist/teetime-automator-web

# Run unit tests
ng test

# Run end-to-end tests
ng e2e

# Code linting
ng lint

# Format code
ng lint --fix

# Generate a new component
ng generate component pages/my-feature/my-component

# Generate a new service
ng generate service services/my-service

# Deploy to production
ng build --configuration production
```

## Key Features

### 1. Authentication
- Email/Password login
- User registration
- Google Sign-In (configure your credentials)
- JWT token management
- Automatic logout on token expiration

### 2. Booking Management
- Create bookings with multi-step wizard
- View and manage bookings
- Cancel or retry bookings
- Filter bookings by status
- See booking timeline and results

### 3. Course Management
- Add new golf courses
- Configure release schedules
- Store booking credentials securely
- Edit and delete courses

### 4. User Profile
- Update personal information
- Change password
- View connected courses

### 5. Admin Dashboard
- View system statistics
- Manage users (enable/disable)
- View all bookings
- See recent activity

## Navigation

### Public Routes (No Authentication Required)
- `/login` - Login page
- `/register` - Registration page

### Protected Routes (Authentication Required)
- `/dashboard` - User dashboard
- `/bookings` - List of bookings
- `/bookings/new` - Create new booking
- `/bookings/:id` - Booking details
- `/courses` - List of courses
- `/courses/new` - Add new course
- `/profile` - User profile

### Admin Routes (Admin Authentication Required)
- `/admin` - Admin dashboard
- `/admin/users` - User management
- `/admin/bookings` - All bookings

## Testing Locally

### 1. Create Test Account
1. Click "Create one now" on login page
2. Fill in registration form
3. You'll be logged in automatically

### 2. Add a Test Course
1. Navigate to Courses
2. Click "Add Course"
3. Fill in course details (you can use any URL for testing)
4. Create booking for this course

### 3. Create Test Booking
1. Navigate to Bookings
2. Click "New Booking"
3. Follow the multi-step wizard
4. Review and create booking

## Styling & Theme

### Colors
- Primary Green: `#2e7d32`
- Dark Green: `#0d3817`
- Light Green: `#43a047`
- Dark Background: `#121212`

### Customizing Theme
Edit `src/styles.scss` to change colors:

```scss
$golf-green: #1b5e20;
$golf-light-green: #2e7d32;
$golf-accent-green: #43a047;
```

## Debugging

### Console Logs
Open browser DevTools (F12) to see:
- HTTP requests in Network tab
- JavaScript errors in Console tab
- Application state in Application tab

### Common Issues

**401 Unauthorized Error**
- Token expired: Log in again
- Invalid token: Check `src/environments/environment.ts` API URL

**CORS Error**
- Backend not allowing requests from frontend domain
- Configure CORS in backend (allow http://localhost:4200)

**API Connection Error**
- Backend not running
- Wrong API URL in environment config
- Network connectivity issue

### Angular DevTools
Install [Angular DevTools Chrome Extension](https://chrome.google.com/webstore/detail/angular-devtools/ienfalfjdbdpebioblfackkekamfmbco) for advanced debugging.

## Performance Tips

1. **Use Production Build**: `ng build --configuration production`
2. **Enable AOT**: Already enabled by default
3. **Lazy Load Routes**: Routes are configured for lazy loading
4. **Use OnPush Detection**: Components support change detection optimization
5. **Tree Shaking**: Only used code is bundled

## Security Checklist

- [ ] Change API endpoints to production URLs
- [ ] Configure Google OAuth credentials
- [ ] Enable HTTPS in production
- [ ] Set secure cookie flags
- [ ] Review JWT token expiration
- [ ] Enable CORS only for your domain
- [ ] Use strong password requirements
- [ ] Enable rate limiting on backend

## Deployment

### Build for Production
```bash
ng build --configuration production
```

### Deploy Options

#### Static Hosting (Vercel, Netlify, Firebase)
1. Build: `ng build`
2. Upload `dist/teetime-automator-web` folder
3. Configure API endpoints via environment files

#### Docker
Create `Dockerfile`:
```dockerfile
FROM node:18 AS build
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build

FROM nginx:alpine
COPY --from=build /app/dist/teetime-automator-web /usr/share/nginx/html
```

#### Traditional Server
1. Build: `ng build`
2. Copy `dist/teetime-automator-web` to `/var/www/`
3. Configure nginx/apache to serve files

## Environment Checklist

### Development
- [ ] Node.js 18+ installed
- [ ] npm 9+ installed
- [ ] Angular CLI 17 installed
- [ ] Backend API running on localhost:5000
- [ ] Correct API URL in environment.ts

### Production
- [ ] Production build created: `ng build`
- [ ] API URL points to production backend
- [ ] Google OAuth credentials configured
- [ ] HTTPS enabled
- [ ] CORS properly configured on backend
- [ ] Security headers configured

## Getting Help

### Common Questions

**Q: How do I use Google Sign-In?**
A: Configure Google OAuth credentials in your Google Cloud Console and add the Client ID to both environment files.

**Q: How do I customize colors?**
A: Edit `src/styles.scss` and change the color variables at the top.

**Q: How do I add a new page?**
A: Run `ng generate component pages/my-page` and add route to `app.routes.ts`.

**Q: How do I modify API calls?**
A: Update the service files in `src/app/services/`.

## Next Steps

1. ✅ Get it running locally
2. ✅ Create test data
3. ✅ Explore admin dashboard
4. ✅ Test all booking flows
5. ✅ Customize styling
6. ✅ Configure authentication
7. ✅ Deploy to production

## Support

- Check `README.md` for detailed documentation
- Check `PROJECT_SUMMARY.md` for feature list
- Review Angular docs: https://angular.io
- Review Angular Material docs: https://material.angular.io

---

**Happy Golf Booking! ⛳**
