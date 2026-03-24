import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BookingService } from '../../../services/booking.service';
import { CourseService } from '../../../services/course.service';
import { AuthService } from '../../../services/auth.service';
import { BookingRequest, BookingStatus, getStatusIcon, getStatusLabel } from '../../../models/booking.models';
import { StatusChipComponent } from '../../../shared/status-chip/status-chip.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTableModule,
    MatProgressSpinnerModule,
    StatusChipComponent
  ],
  template: `
    <div class="dashboard-container">
      <div class="dashboard-header">
        <h1>Welcome, {{ (currentUser$ | async)?.firstName }}!</h1>
        <p>Manage your golf tee time bookings with ease</p>
      </div>

      <div class="stats-grid">
        <mat-card class="stat-card">
          <div class="stat-icon active">
            <mat-icon>calendar_today</mat-icon>
          </div>
          <div class="stat-content">
            <div class="stat-label">Active Bookings</div>
            <div class="stat-value">{{ activeBookingCount }}</div>
          </div>
        </mat-card>

        <mat-card class="stat-card">
          <div class="stat-icon booked">
            <mat-icon>check_circle</mat-icon>
          </div>
          <div class="stat-content">
            <div class="stat-label">Upcoming Tee Times</div>
            <div class="stat-value">{{ upcomingBookingCount }}</div>
          </div>
        </mat-card>

        <mat-card class="stat-card">
          <div class="stat-icon configured">
            <mat-icon>location_on</mat-icon>
          </div>
          <div class="stat-content">
            <div class="stat-label">Courses Configured</div>
            <div class="stat-value">{{ courseCount }}</div>
          </div>
        </mat-card>

        <mat-card class="stat-card">
          <div class="stat-icon success">
            <mat-icon>trending_up</mat-icon>
          </div>
          <div class="stat-content">
            <div class="stat-label">Success Rate</div>
            <div class="stat-value">{{ successRate }}%</div>
          </div>
        </mat-card>
      </div>

      <div class="content-section">
        <div class="section-header">
          <h2>Recent Bookings</h2>
          <button mat-raised-button color="primary" routerLink="/bookings/new">
            <mat-icon>add</mat-icon>
            Book a Tee Time
          </button>
        </div>

        <div *ngIf="loading" class="loading-spinner">
          <mat-spinner diameter="50"></mat-spinner>
        </div>

        <div *ngIf="!loading && recentBookings.length === 0" class="empty-state">
          <mat-icon>golf_course</mat-icon>
          <p>No bookings yet. Start by booking your first tee time!</p>
          <button mat-raised-button color="primary" routerLink="/bookings/new">
            Create Booking
          </button>
        </div>

        <mat-card *ngIf="!loading && recentBookings.length > 0" class="bookings-card">
          <table mat-table [dataSource]="recentBookings" class="bookings-table">
            <!-- Course Column -->
            <ng-container matColumnDef="course">
              <th mat-header-cell *matHeaderCellDef>Course</th>
              <td mat-cell *matCellDef="let element">
                {{ element.courseName }}
              </td>
            </ng-container>

            <!-- Date Column -->
            <ng-container matColumnDef="date">
              <th mat-header-cell *matHeaderCellDef>Date</th>
              <td mat-cell *matCellDef="let element">
                {{ element.requestedDate | date: 'MMM dd, yyyy' }}
              </td>
            </ng-container>

            <!-- Time Column -->
            <ng-container matColumnDef="time">
              <th mat-header-cell *matHeaderCellDef>Time</th>
              <td mat-cell *matCellDef="let element">
                {{ element.preferredTime }}
              </td>
            </ng-container>

            <!-- Players Column -->
            <ng-container matColumnDef="players">
              <th mat-header-cell *matHeaderCellDef>Players</th>
              <td mat-cell *matCellDef="let element">
                {{ element.numberOfPlayers }}
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let element">
                <app-status-chip [status]="element.status"></app-status-chip>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let element">
                <button
                  mat-icon-button
                  [routerLink]="['/bookings', element.id]"
                  matTooltip="View Details"
                >
                  <mat-icon>visibility</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>
        </mat-card>

        <div class="view-all-link" *ngIf="recentBookings.length > 0">
          <a routerLink="/bookings" class="link">View all bookings</a>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .dashboard-header {
      margin-bottom: 3rem;

      h1 {
        font-size: 2rem;
        color: #c8e6c9;
        margin-bottom: 0.5rem;
      }

      p {
        color: #999999;
        font-size: 1.05rem;
      }
    }

    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 2rem;
      margin-bottom: 3rem;
    }

    .stat-card {
      display: flex;
      align-items: center;
      padding: 1.5rem;
      background-color: #1e1e1e !important;
      border-radius: 8px;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 8px 16px rgba(46, 125, 50, 0.15);
      }
    }

    .stat-icon {
      width: 60px;
      height: 60px;
      border-radius: 8px;
      display: flex;
      align-items: center;
      justify-content: center;
      margin-right: 1.5rem;
      flex-shrink: 0;

      mat-icon {
        font-size: 32px;
        width: 32px;
        height: 32px;
      }

      &.active {
        background-color: rgba(33, 150, 243, 0.1);
        color: #2196f3;
      }

      &.booked {
        background-color: rgba(76, 175, 80, 0.1);
        color: #4caf50;
      }

      &.configured {
        background-color: rgba(46, 125, 50, 0.1);
        color: #2e7d32;
      }

      &.success {
        background-color: rgba(255, 152, 0, 0.1);
        color: #ff9800;
      }
    }

    .stat-content {
      flex: 1;

      .stat-label {
        color: #999999;
        font-size: 0.9rem;
        text-transform: uppercase;
        letter-spacing: 0.5px;
        margin-bottom: 0.5rem;
      }

      .stat-value {
        color: #c8e6c9;
        font-size: 2rem;
        font-weight: 600;
      }
    }

    .content-section {
      margin-top: 3rem;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h2 {
        margin: 0;
      }
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .bookings-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      overflow: hidden;
    }

    .bookings-table {
      width: 100%;

      th {
        background-color: #2a2a2a;
        color: #c8e6c9;
        font-weight: 600;
        padding: 1rem;
      }

      td {
        padding: 1rem;
        border-bottom: 1px solid #303030;
        color: #ffffff;
      }

      tr:last-child td {
        border-bottom: none;
      }

      tr:hover {
        background-color: #252525;
      }
    }

    .view-all-link {
      text-align: right;
      margin-top: 1.5rem;

      .link {
        color: #43a047;
        text-decoration: none;
        font-weight: 600;
        cursor: pointer;

        &:hover {
          text-decoration: underline;
        }
      }
    }

    @media (max-width: 768px) {
      .dashboard-container {
        padding: 1rem;
      }

      .dashboard-header h1 {
        font-size: 1.5rem;
      }

      .stats-grid {
        grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
        gap: 1rem;
      }

      .stat-card {
        flex-direction: column;
        text-align: center;
        padding: 1rem;
      }

      .stat-icon {
        margin-right: 0;
        margin-bottom: 1rem;
      }

      .section-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .bookings-table {
        font-size: 0.85rem;

        th, td {
          padding: 0.75rem;
        }
      }
    }
  `]
})
export class DashboardComponent implements OnInit {
  currentUser$ = this.authService.currentUser$;
  recentBookings: BookingRequest[] = [];
  loading = false;

  activeBookingCount = 0;
  upcomingBookingCount = 0;
  courseCount = 0;
  successRate = 0;

  displayedColumns = ['course', 'date', 'time', 'players', 'status', 'actions'];

  constructor(
    private bookingService: BookingService,
    private courseService: CourseService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    this.bookingService.getBookings().subscribe({
      next: (bookings) => {
        this.recentBookings = bookings.slice(0, 5);

        this.activeBookingCount = bookings.filter((b) =>
          [BookingStatus.SCHEDULED, BookingStatus.IN_PROGRESS].includes(b.status)
        ).length;

        this.upcomingBookingCount = bookings.filter(
          (b) => b.status === BookingStatus.BOOKED
        ).length;

        const bookedCount = bookings.filter(
          (b) => b.status === BookingStatus.BOOKED
        ).length;
        const totalCount = bookings.length;
        this.successRate =
          totalCount > 0 ? Math.round((bookedCount / totalCount) * 100) : 0;

        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });

    this.courseService.getCourses().subscribe({
      next: (courses) => {
        this.courseCount = courses.length;
      }
    });
  }
}
