import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
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
import { finalize } from 'rxjs/operators';
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
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
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
    private authService: AuthService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    this.loading = true;

    this.bookingService.getBookings().pipe(
      finalize(() => { this.loading = false; this.cdr.detectChanges(); })
    ).subscribe({
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
      },
      error: () => {}
    });

    this.courseService.getCourses().subscribe({
      next: (courses) => {
        this.courseCount = courses.length;
      }
    });
  }
}
