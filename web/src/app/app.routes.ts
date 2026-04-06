import { Routes } from '@angular/router';
import { AuthGuard } from './guards/auth.guard';
import { AdminGuard } from './guards/admin.guard';

export const appRoutes: Routes = [
  {
    path: '',
    redirectTo: 'dashboard',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./pages/auth/login/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./pages/auth/register/register.component').then(
        (m) => m.RegisterComponent
      )
  },
  {
    path: 'auth/google-callback',
    loadComponent: () =>
      import('./pages/auth/google-callback/google-callback.component').then(
        (m) => m.GoogleCallbackComponent
      )
  },
  {
    path: 'dashboard',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/dashboard/dashboard/dashboard.component').then(
        (m) => m.DashboardComponent
      )
  },
  {
    path: 'bookings',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/bookings/booking-list/booking-list.component').then(
        (m) => m.BookingListComponent
      )
  },
  {
    path: 'bookings/new',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/bookings/booking-create/booking-create.component').then(
        (m) => m.BookingCreateComponent
      )
  },
  {
    path: 'bookings/:id',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/bookings/booking-detail/booking-detail.component').then(
        (m) => m.BookingDetailComponent
      )
  },
  {
    path: 'courses',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/courses/course-list/course-list.component').then(
        (m) => m.CourseListComponent
      )
  },
  {
    path: 'courses/new',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/courses/course-form/course-form.component').then(
        (m) => m.CourseFormComponent
      )
  },
  {
    path: 'courses/:id/edit',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/courses/course-form/course-form.component').then(
        (m) => m.CourseFormComponent
      )
  },
  {
    path: 'profile',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./pages/profile/profile/profile.component').then(
        (m) => m.ProfileComponent
      )
  },
  {
    path: 'admin',
    canActivate: [AdminGuard],
    loadComponent: () =>
      import('./pages/admin/admin-dashboard/admin-dashboard.component').then(
        (m) => m.AdminDashboardComponent
      )
  },
  {
    path: 'admin/users',
    canActivate: [AdminGuard],
    loadComponent: () =>
      import('./pages/admin/admin-users/admin-users.component').then(
        (m) => m.AdminUsersComponent
      )
  },
  {
    path: 'admin/bookings',
    canActivate: [AdminGuard],
    loadComponent: () =>
      import('./pages/admin/admin-bookings/admin-bookings.component').then(
        (m) => m.AdminBookingsComponent
      )
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];
