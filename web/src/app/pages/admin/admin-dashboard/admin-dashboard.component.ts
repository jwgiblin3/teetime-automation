import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { AdminService } from '../../../services/admin.service';
import { finalize } from 'rxjs/operators';
import { SystemStats, AuditLog } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule
  ],
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css']
})
export class AdminDashboardComponent implements OnInit {
  stats: SystemStats | null = null;
  activityLogs: AuditLog[] = [];
  loading = false;

  constructor(private adminService: AdminService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    this.adminService.getStats().pipe(
      finalize(() => { this.cdr.detectChanges(); })
    ).subscribe({
      next: (stats) => { this.stats = stats; },
      error: () => {}
    });

    this.adminService.getRecentActivity(10).pipe(
      finalize(() => { this.loading = false; this.cdr.detectChanges(); })
    ).subscribe({
      next: (logs) => { this.activityLogs = logs; },
      error: () => {}
    });
  }

  getActivityIcon(action: string): string {
    const icons: Record<string, string> = {
      create: 'add_circle',
      update: 'edit',
      delete: 'delete',
      login: 'login',
      logout: 'logout',
      booking: 'calendar_today',
      default: 'info'
    };
    return icons[action] || icons['default'];
  }

  getActivityTitle(action: string, resourceType: string): string {
    const titles: Record<string, Record<string, string>> = {
      create: {
        booking: 'Booking Created',
        course: 'Course Added',
        user: 'User Registered',
        default: 'Created'
      },
      update: {
        booking: 'Booking Updated',
        course: 'Course Updated',
        user: 'User Updated',
        default: 'Updated'
      },
      delete: {
        booking: 'Booking Deleted',
        course: 'Course Deleted',
        user: 'User Deleted',
        default: 'Deleted'
      },
      login: {
        user: 'User Logged In',
        default: 'Logged In'
      },
      logout: {
        user: 'User Logged Out',
        default: 'Logged Out'
      }
    };

    return (titles[action] && titles[action][resourceType]) || titles[action]?.['default'] || 'Activity';
  }
}
