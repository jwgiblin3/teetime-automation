import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { BookingService } from '../../../services/booking.service';
import { BookingRequest, BookingStatus, getStatusIcon, getStatusLabel } from '../../../models/booking.models';
import { StatusChipComponent } from '../../../shared/status-chip/status-chip.component';

@Component({
  selector: 'app-booking-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTabsModule,
    StatusChipComponent
  ],
  templateUrl: './booking-detail.component.html',
  styleUrls: ['./booking-detail.component.css']
})
export class BookingDetailComponent implements OnInit {
  booking: BookingRequest | null = null;
  loading = true;
  retrying = false;
  cancelling = false;
  BookingStatus = BookingStatus;

  constructor(
    private route: ActivatedRoute,
    private bookingService: BookingService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBooking();
  }

  loadBooking(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.router.navigate(['/bookings']);
      return;
    }

    this.bookingService.getBooking(id).pipe(
      finalize(() => { this.loading = false; this.cdr.detectChanges(); })
    ).subscribe({
      next: (booking) => { this.booking = booking; },
      error: () => {}
    });
  }

  retryBooking(): void {
    if (!this.booking?.id) return;

    this.retrying = true;
    this.bookingService.retryBooking(this.booking.id).pipe(
      finalize(() => { this.retrying = false; this.cdr.detectChanges(); })
    ).subscribe({
      next: (updatedBooking) => { this.booking = updatedBooking; },
      error: () => {}
    });
  }

  cancelBooking(): void {
    if (!this.booking?.id) return;

    if (confirm('Are you sure you want to cancel this booking?')) {
      this.cancelling = true;
      this.bookingService.cancelBooking(this.booking.id).subscribe({
        next: () => {
          this.router.navigate(['/bookings']);
        },
        error: () => {
          this.cancelling = false;
        }
      });
    }
  }
}
