import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';
import { MatMenuModule } from '@angular/material/menu';
import { CourseService } from '../../../services/course.service';
import { finalize } from 'rxjs/operators';
import { Course, getPlatformLabel } from '../../../models/course.models';

@Component({
  selector: 'app-course-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
    MatMenuModule
  ],
  template: `
    <div class="courses-container">
      <div class="courses-header">
        <h1>My Courses</h1>
        <button mat-raised-button color="primary" routerLink="/courses/new">
          <mat-icon>add</mat-icon>
          Add Course
        </button>
      </div>

      <div *ngIf="loading" class="loading-spinner">
        <mat-spinner diameter="50"></mat-spinner>
      </div>

      <div *ngIf="!loading && courses.length === 0" class="empty-state">
        <mat-icon>golf_course</mat-icon>
        <p>No courses configured yet</p>
        <p class="empty-description">Add a course to start automating your tee time bookings</p>
        <button mat-raised-button color="primary" routerLink="/courses/new">
          Add Your First Course
        </button>
      </div>

      <div class="courses-grid" *ngIf="!loading && courses.length > 0">
        <mat-card class="course-card" *ngFor="let course of courses">
          <mat-card-header>
            <div class="card-header-content">
              <mat-card-title>{{ course.name }}</mat-card-title>
              <mat-card-subtitle>
                <mat-chip class="platform-badge">
                  {{ getPlatformLabel(course.platform) }}
                </mat-chip>
              </mat-card-subtitle>
            </div>
            <button
              mat-icon-button
              [matMenuTriggerFor]="menu"
              class="menu-button"
            >
              <mat-icon>more_vert</mat-icon>
            </button>
            <mat-menu #menu="matMenu">
              <button mat-menu-item [routerLink]="['/courses', course.id, 'edit']">
                <mat-icon>edit</mat-icon>
                <span>Edit</span>
              </button>
              <button mat-menu-item (click)="deleteCourse(course.id)">
                <mat-icon>delete</mat-icon>
                <span>Delete</span>
              </button>
            </mat-menu>
          </mat-card-header>

          <mat-card-content>
            <div class="course-details">
              <div class="detail-row">
                <span class="label">Booking URL:</span>
                <a [href]="course.bookingUrl" target="_blank" class="url-link">
                  {{ truncateUrl(course.bookingUrl) }}
                  <mat-icon>open_in_new</mat-icon>
                </a>
              </div>

              <div class="detail-row">
                <span class="label">Release Schedule:</span>
                <span class="value">
                  {{ course.releaseSchedule.daysBeforeRelease }} days before at
                  {{ formatTime(course.releaseSchedule.releaseTimeHour, course.releaseSchedule.releaseTimeMinute) }}
                </span>
              </div>

              <div class="detail-row">
                <span class="label">Credentials:</span>
                <span class="credential-status" [class.saved]="course.credentialsSaved">
                  <mat-icon>{{ course.credentialsSaved ? 'check_circle' : 'warning' }}</mat-icon>
                  {{ course.credentialsSaved ? 'Saved' : 'Not configured' }}
                </span>
              </div>
            </div>
          </mat-card-content>

          <mat-card-actions>
            <button mat-button color="accent" [routerLink]="['/bookings/new']">
              <mat-icon>add_circle_outline</mat-icon>
              Book Now
            </button>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .courses-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .courses-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .courses-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
      gap: 2rem;
    }

    .course-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      transition: transform 0.2s, box-shadow 0.2s;

      &:hover {
        transform: translateY(-4px);
        box-shadow: 0 12px 24px rgba(46, 125, 50, 0.15);
      }
    }

    mat-card-header {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      margin-bottom: 1rem;
    }

    .card-header-content {
      flex: 1;
    }

    mat-card-title {
      color: #c8e6c9 !important;
      margin-bottom: 0.5rem;
    }

    mat-card-subtitle {
      color: #999999 !important;
    }

    .platform-badge {
      background-color: #2e7d32 !important;
      color: #ffffff !important;
      height: 24px;
      font-size: 0.75rem;
      margin-top: 0.5rem;
    }

    .menu-button {
      color: #999999 !important;

      &:hover {
        color: #c8e6c9 !important;
      }
    }

    .course-details {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      margin-bottom: 1.5rem;
    }

    .detail-row {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .label {
      color: #999999;
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;
    }

    .value {
      color: #c8e6c9;
      font-size: 0.95rem;
    }

    .url-link {
      color: #43a047;
      text-decoration: none;
      display: flex;
      align-items: center;
      gap: 0.25rem;
      word-break: break-all;

      &:hover {
        text-decoration: underline;
      }

      mat-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
        flex-shrink: 0;
      }
    }

    .credential-status {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #ff9800;

      &.saved {
        color: #4caf50;
      }

      mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
    }

    mat-card-actions {
      padding-top: 1rem;
      border-top: 1px solid #303030;
    }

    @media (max-width: 768px) {
      .courses-container {
        padding: 1rem;
      }

      .courses-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .courses-grid {
        grid-template-columns: 1fr;
      }

      button {
        width: 100%;
      }
    }
  `]
})
export class CourseListComponent implements OnInit {
  courses: Course[] = [];
  loading = false;

  constructor(
    private courseService: CourseService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadCourses();
  }

  loadCourses(): void {
    this.loading = true;
    this.courseService.getCourses().pipe(
      finalize(() => { this.loading = false; this.cdr.detectChanges(); })
    ).subscribe({
      next: (courses) => { this.courses = courses; },
      error: () => {}
    });
  }

  deleteCourse(id: string): void {
    if (confirm('Are you sure you want to delete this course?')) {
      this.courseService.deleteCourse(id).subscribe({
        next: () => {
          this.loadCourses();
        }
      });
    }
  }

  getPlatformLabel(platform: string): string {
    return getPlatformLabel(platform as any);
  }

  truncateUrl(url: string): string {
    if (url.length > 40) {
      return url.substring(0, 40) + '...';
    }
    return url;
  }

  formatTime(hour: number, minute: number): string {
    const h = String(hour).padStart(2, '0');
    const m = String(minute).padStart(2, '0');
    return `${h}:${m}`;
  }
}


