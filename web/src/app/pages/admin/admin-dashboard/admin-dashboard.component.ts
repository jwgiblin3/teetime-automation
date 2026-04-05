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
  template: `
    <div class="admin-container">
      <div class="admin-header">
        <h1>Admin Dashboard</h1>
        <div class="header-actions">
          <button mat-button routerLink="/admin/users">
            <mat-icon>people</mat-icon>
            Users
          </button>
          <button mat-button routerLink="/admin/bookings">
            <mat-icon>calendar_today</mat-icon>
            Bookings
          </button>
        </div>
      </div>

      <div *ngIf="loading" class="loading-spinner">
        <mat-spinner diameter="50"></mat-spinner>
      </div>

      <div *ngIf="!loading && !stats" class="empty-state">
        <mat-icon>bar_chart</mat-icon>
        <p>No stats available. The API may be unreachable.</p>
      </div>

      <div *ngIf="!loading && stats" class="stats-section">
        <div class="stats-grid">
          <mat-card class="stat-card">
            <div class="stat-icon users">
              <mat-icon>people</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Total Users</div>
              <div class="stat-value">{{ stats.totalUsers }}</div>
            </div>
          </mat-card>

          <mat-card class="stat-card">
            <div class="stat-icon bookings">
              <mat-icon>calendar_today</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Total Bookings</div>
              <div class="stat-value">{{ stats.totalBookings }}</div>
            </div>
          </mat-card>

          <mat-card class="stat-card">
            <div class="stat-icon success">
              <mat-icon>check_circle</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Success Rate</div>
              <div class="stat-value">{{ stats.successRate }}%</div>
            </div>
          </mat-card>

          <mat-card class="stat-card">
            <div class="stat-icon active">
              <mat-icon>sync</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Active Polls</div>
              <div class="stat-value">{{ stats.activePolls }}</div>
            </div>
          </mat-card>

          <mat-card class="stat-card">
            <div class="stat-icon booked">
              <mat-icon>done_all</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Successful Bookings</div>
              <div class="stat-value">{{ stats.successfulBookings }}</div>
            </div>
          </mat-card>

          <mat-card class="stat-card">
            <div class="stat-icon failed">
              <mat-icon>error</mat-icon>
            </div>
            <div class="stat-content">
              <div class="stat-label">Failed Bookings</div>
              <div class="stat-value">{{ stats.failedBookings }}</div>
            </div>
          </mat-card>
        </div>
      </div>

      <!-- Recent Activity Section -->
      <mat-card class="activity-card" *ngIf="!loading && activityLogs.length > 0">
        <mat-card-header>
          <mat-card-title>Recent Activity</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div class="activity-feed">
            <div class="activity-item" *ngFor="let log of activityLogs">
              <div class="activity-icon">
                <mat-icon [class]="log.action">
                  {{ getActivityIcon(log.action) }}
                </mat-icon>
              </div>
              <div class="activity-details">
                <div class="activity-title">
                  {{ getActivityTitle(log.action, log.resourceType) }}
                </div>
                <div class="activity-description">
                  {{ log.resourceId }}
                </div>
                <div class="activity-timestamp">
                  {{ log.timestamp | date: 'MMM dd, HH:mm' }}
                </div>
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>

      <!-- Quick Stats -->
      <mat-card class="quick-stats-card" *ngIf="!loading && stats">
        <mat-card-header>
          <mat-card-title>Performance Metrics</mat-card-title>
        </mat-card-header>

        <mat-card-content>
          <div class="metrics-grid">
            <div class="metric">
              <span class="metric-label">Avg Booking Time</span>
              <span class="metric-value">{{ stats.averageBookingTime }}m</span>
            </div>

            <div class="metric">
              <span class="metric-label">Active Courses</span>
              <span class="metric-value">{{ stats.successfulBookings === 0 ? 0 : (stats.successfulBookings | number) }}</span>
            </div>

            <div class="metric">
              <span class="metric-label">Bookings/User (avg)</span>
              <span class="metric-value">{{ stats.totalUsers > 0 ? (stats.totalBookings / stats.totalUsers | number: '1.1-1') : 0 }}</span>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .admin-container {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .admin-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }

      .header-actions {
        display: flex;
        gap: 1rem;
      }
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .stats-section {
      margin-bottom: 3rem;
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .stat-card {
      background-color: #1e1e1e !important;
      display: flex;
      align-items: center;
      padding: 1.5rem;
      border-radius: 8px;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 16px rgba(46, 125, 50, 0.15);
      }
    }

    .stat-icon {
      width: 50px;
      height: 50px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 1.5rem;
      flex-shrink: 0;

      mat-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }

      &.users {
        background-color: rgba(33, 150, 243, 0.1);
        color: #2196f3;
      }

      &.bookings {
        background-color: rgba(156, 39, 176, 0.1);
        color: #9c27b0;
      }

      &.success {
        background-color: rgba(76, 175, 80, 0.1);
        color: #4caf50;
      }

      &.active {
        background-color: rgba(255, 193, 7, 0.1);
        color: #ffc107;
      }

      &.booked {
        background-color: rgba(46, 125, 50, 0.1);
        color: #2e7d32;
      }

      &.failed {
        background-color: rgba(211, 47, 47, 0.1);
        color: #d32f2f;
      }
    }

    .stat-content {
      flex: 1;
    }

    .stat-label {
      color: #999999;
      font-size: 0.9rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-bottom: 0.5rem;
    }

    .stat-value {
      color: #c8e6c9;
      font-size: 1.75rem;
      font-weight: 600;
    }

    .activity-card,
    .quick-stats-card {
      background-color: #1e1e1e !important;
      margin-bottom: 2rem;
    }

    mat-card-header {
      margin-bottom: 1.5rem;
    }

    mat-card-title {
      color: #c8e6c9 !important;
    }

    .activity-feed {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      max-height: 400px;
      overflow-y: auto;
    }

    .activity-item {
      display: flex;
      gap: 1rem;
      padding: 1rem;
      background-color: #252525;
      border-radius: 4px;
      align-items: flex-start;
    }

    .activity-icon {
      mat-icon {
        font-size: 24px;
        width: 24px;
        height: 24px;
        color: #2e7d32;

        &.create,
        &.update {
          color: #2196f3;
        }

        &.delete {
          color: #d32f2f;
        }
      }
    }

    .activity-details {
      flex: 1;
    }

    .activity-title {
      color: #c8e6c9;
      font-weight: 600;
      margin-bottom: 0.25rem;
    }

    .activity-description {
      color: #999999;
      font-size: 0.9rem;
    }

    .activity-timestamp {
      color: #666666;
      font-size: 0.8rem;
      margin-top: 0.25rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 2rem;
    }

    .metric {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 0.5rem;
      padding: 1rem;
      background-color: #252525;
      border-radius: 4px;
    }

    .metric-label {
      color: #999999;
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;
    }

    .metric-value {
      color: #c8e6c9;
      font-size: 1.5rem;
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .admin-container {
        padding: 1rem;
      }

      .admin-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .stats-grid {
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      }
    }
  `]
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
