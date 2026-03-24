import { User } from './auth.models';
import { BookingRequest } from './booking.models';
import { Course } from './course.models';

export interface AdminUser extends User {
  bookingCount: number;
  lastActivity: Date;
  isDisabled: boolean;
}

export interface AdminBooking extends BookingRequest {
  userName: string;
  userEmail: string;
}

export interface SystemStats {
  totalUsers: number;
  totalBookings: number;
  successfulBookings: number;
  failedBookings: number;
  activeCourses: number;
  successRate: number;
  activePolls: number;
  averageBookingTime: number; // in minutes
}

export interface AuditLog {
  id: string;
  userId: string;
  action: string;
  resourceType: string;
  resourceId: string;
  details: Record<string, unknown>;
  ipAddress: string;
  timestamp: Date;
}
