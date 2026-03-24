import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { AdminUser, SystemStats, AdminBooking, AuditLog } from '../models/admin.models';
import { User } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AdminService extends ApiService {
  private adminUrl = '/admin';

  constructor(http: HttpClient) {
    super(http);
  }

  getUsers(page: number = 1, limit: number = 20): Observable<{ users: AdminUser[]; total: number }> {
    const params = this.buildParams({ page: String(page), limit: String(limit) });
    return this.http.get<{ users: AdminUser[]; total: number }>(
      this.buildUrl(`${this.adminUrl}/users`),
      { params }
    );
  }

  getUser(id: string): Observable<AdminUser> {
    return this.http.get<AdminUser>(this.buildUrl(`${this.adminUrl}/users/${id}`));
  }

  disableUser(id: string): Observable<void> {
    return this.http.post<void>(
      this.buildUrl(`${this.adminUrl}/users/${id}/disable`),
      {}
    );
  }

  enableUser(id: string): Observable<void> {
    return this.http.post<void>(
      this.buildUrl(`${this.adminUrl}/users/${id}/enable`),
      {}
    );
  }

  getCourses(): Observable<any[]> {
    return this.http.get<any[]>(this.buildUrl(`${this.adminUrl}/courses`));
  }

  getBookings(page: number = 1, limit: number = 20): Observable<{ bookings: AdminBooking[]; total: number }> {
    const params = this.buildParams({ page: String(page), limit: String(limit) });
    return this.http.get<{ bookings: AdminBooking[]; total: number }>(
      this.buildUrl(`${this.adminUrl}/bookings`),
      { params }
    );
  }

  getStats(): Observable<SystemStats> {
    return this.http.get<SystemStats>(this.buildUrl(`${this.adminUrl}/stats`));
  }

  getLogs(page: number = 1, limit: number = 20): Observable<{ logs: AuditLog[]; total: number }> {
    const params = this.buildParams({ page: String(page), limit: String(limit) });
    return this.http.get<{ logs: AuditLog[]; total: number }>(
      this.buildUrl(`${this.adminUrl}/logs`),
      { params }
    );
  }

  getRecentActivity(limit: number = 10): Observable<AuditLog[]> {
    const params = this.buildParams({ limit: String(limit) });
    return this.http.get<AuditLog[]>(
      this.buildUrl(`${this.adminUrl}/activity`),
      { params }
    );
  }

  exportBookingData(format: 'csv' | 'json' = 'csv'): Observable<Blob> {
    return this.http.get(
      this.buildUrl(`${this.adminUrl}/export/bookings?format=${format}`),
      { responseType: 'blob' }
    );
  }

  exportUserData(format: 'csv' | 'json' = 'csv'): Observable<Blob> {
    return this.http.get(
      this.buildUrl(`${this.adminUrl}/export/users?format=${format}`),
      { responseType: 'blob' }
    );
  }
}
