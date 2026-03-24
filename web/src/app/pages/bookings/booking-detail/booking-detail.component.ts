import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
  template: `
    <div class="detail-container">
      <div class="detail-header">
        <button mat-icon-button routerLink="/bookings" class="back-button">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <h1>Booking Details</h1>
        <div class="spacer"></div>
      </div>

      <div *ngIf="loading" class="loading-spinner">
        <mat-spinner diameter="50"></mat-spinner>
      </div>

      <div *ngIf="!loading && booking" class="booking-details">
        <!-- Main Details -->
        <mat-card class="details-card">
          <mat-card-header>
            <mat-card-title>{{ booking.courseName }}</mat-card-title>
            <mat-card-subtitle>
              <app-status-chip [status]="booking.status"></app-status-chip>
            </mat-card-subtitle>
          </mat-card-header>

          <mat-card-content>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="detail-label">
                  <mat-icon>calendar_today</mat-icon>
                  Requested Date
                </span>
                <span class="detail-value">
                  {{ booking.requestedDate | date: 'MMMM dd, yyyy' }}
                </span>
              </div>

              <div class="detail-item">
                <span class="detail-label">
                  <mat-icon>schedule</mat-icon>
                  Preferred Time
                </span>
                <span class="detail-value">{{ booking.preferredTime }}</span>
              </div>

              <div class="detail-item">
                <span class="detail-label">
                  <mat-icon>people</mat-icon>
                  Players
                </span>
                <span class="detail-value">{{ booking.numberOfPlayers }}</span>
              </div>

              <div class="detail-item">
                <span class="detail-label">
                  <mat-icon>schedule</mat-icon>
                  Time Window
                </span>
                <span class="detail-value">±{{ booking.timeWindowMinutes }} minutes</span>
              </div>

              <div class="detail-item">
                <span class="detail-label">
                  <mat-icon>access_time</mat-icon>
                  Attempts
                </span>
                <span class="detail-value">{{ booking.attempts }}</span>
              </div>

              <div class="detail-item" *ngIf="booking.nextAttempt">
                <span class="detail-label">
                  <mat-icon>schedule</mat-icon>
                  Next Attempt
                </span>
                <span class="detail-value">
                  {{ booking.nextAttempt | date: 'MMM dd, HH:mm' }}
                </span>
              </div>
            </div>

            <div class="actions-section">
              <button
                mat-raised-button
                color="accent"
                (click)="retryBooking()"
                [disabled]="booking.status === BookingStatus.BOOKED || retrying"
              >
                <mat-icon *ngIf="!retrying">refresh</mat-icon>
                <mat-spinner
                  *ngIf="retrying"
                  diameter="20"
                  class="inline-spinner"
                ></mat-spinner>
                {{ retrying ? 'Retrying...' : 'Retry Booking' }}
              </button>

              <button
                mat-raised-button
                color="warn"
                (click)="cancelBooking()"
                [disabled]="booking.status === BookingStatus.BOOKED || cancelling"
              >
                <mat-icon *ngIf="!cancelling">close</mat-icon>
                <mat-spinner
                  *ngIf="cancelling"
                  diameter="20"
                  class="inline-spinner"
                ></mat-spinner>
                {{ cancelling ? 'Cancelling...' : 'Cancel Booking' }}
              </button>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Booking Result -->
        <mat-card class="result-card" *ngIf="booking.bookingResult">
          <mat-card-header>
            <mat-card-title>Booking Result</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="result-grid">
              <div class="result-item" *ngIf="booking.bookingResult.confirmationNumber">
                <span class="result-label">Confirmation Number</span>
                <span class="result-value">
                  {{ booking.bookingResult.confirmationNumber }}
                </span>
              </div>

              <div class="result-item" *ngIf="booking.bookingResult.bookingDate">
                <span class="result-label">Booked Date</span>
                <span class="result-value">
                  {{ booking.bookingResult.bookingDate }}
                </span>
              </div>

              <div class="result-item" *ngIf="booking.bookingResult.bookingTime">
                <span class="result-label">Booked Time</span>
                <span class="result-value">
                  {{ booking.bookingResult.bookingTime }}
                </span>
              </div>

              <div class="result-item" *ngIf="booking.bookingResult.playersConfirmed">
                <span class="result-label">Players Confirmed</span>
                <span class="result-value">
                  {{ booking.bookingResult.playersConfirmed }}
                </span>
              </div>

              <div class="result-item" *ngIf="booking.bookingResult.totalCost">
                <span class="result-label">Total Cost</span>
                <span class="result-value">
                  ${{ booking.bookingResult.totalCost }}
                </span>
              </div>

              <div class="result-item" *ngIf="booking.bookingResult.courseContact">
                <span class="result-label">Course Contact</span>
                <span class="result-value">
                  {{ booking.bookingResult.courseContact }}
                </span>
              </div>

              <div class="result-item error" *ngIf="booking.bookingResult.errorMessage">
                <span class="result-label">Error</span>
                <span class="result-value error-text">
                  {{ booking.bookingResult.errorMessage }}
                </span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <!-- Timeline -->
        <mat-card class="timeline-card">
          <mat-card-header>
            <mat-card-title>Booking Timeline</mat-card-title>
          </mat-card-header>

          <mat-card-content>
            <div class="timeline">
              <div class="timeline-item">
                <div class="timeline-marker">
                  <mat-icon>create</mat-icon>
                </div>
                <div class="timeline-content">
                  <div class="timeline-title">Booking Created</div>
                  <div class="timeline-date">
                    {{ booking.createdAt | date: 'MMM dd, yyyy HH:mm' }}
                  </div>
                </div>
              </div>

              <div class="timeline-item" *ngIf="booking.updatedAt && booking.updatedAt !== booking.createdAt">
                <div class="timeline-marker">
                  <mat-icon>update</mat-icon>
                </div>
                <div class="timeline-content">
                  <div class="timeline-title">Last Updated</div>
                  <div class="timeline-date">
                    {{ booking.updatedAt | date: 'MMM dd, yyyy HH:mm' }}
                  </div>
                </div>
              </div>

              <div class="timeline-item" *ngIf="booking.status === BookingStatus.BOOKED">
                <div class="timeline-marker success">
                  <mat-icon>check_circle</mat-icon>
                </div>
                <div class="timeline-content">
                  <div class="timeline-title">Tee Time Booked Successfully!</div>
                  <div class="timeline-date">
                    Check your confirmation details above
                  </div>
                </div>
              </div>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .detail-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    .detail-header {
      display: flex;
      align-items: center;
      gap: 1rem;
      margin-bottom: 2rem;

      h1 {
        flex: 1;
        margin: 0;
      }

      .back-button {
        color: #2e7d32;
      }

      .spacer {
        width: 40px;
      }
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .booking-details {
      display: flex;
      flex-direction: column;
      gap: 2rem;
    }

    .details-card,
    .result-card,
    .timeline-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
    }

    mat-card-header {
      margin-bottom: 1.5rem;
    }

    mat-card-title {
      color: #c8e6c9 !important;
    }

    .detail-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .detail-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .detail-label {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      color: #999999;
      font-size: 0.9rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;

      mat-icon {
        font-size: 18px;
        width: 18px;
        height: 18px;
      }
    }

    .detail-value {
      color: #c8e6c9;
      font-size: 1.15rem;
      font-weight: 500;
    }

    .actions-section {
      display: flex;
      gap: 1rem;
      margin-top: 1.5rem;
      padding-top: 1.5rem;
      border-top: 1px solid #303030;
    }

    .result-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
      gap: 1.5rem;
    }

    .result-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;

      &.error {
        background-color: rgba(211, 47, 47, 0.1);
        padding: 1rem;
        border-radius: 4px;
        border-left: 4px solid #d32f2f;
      }
    }

    .result-label {
      color: #999999;
      font-size: 0.85rem;
      text-transform: uppercase;
      letter-spacing: 0.5px;
      font-weight: 600;
    }

    .result-value {
      color: #c8e6c9;
      font-size: 1rem;
      font-weight: 500;

      &.error-text {
        color: #ff6b6b;
      }
    }

    .timeline {
      position: relative;
      padding-left: 30px;
    }

    .timeline-item {
      display: flex;
      gap: 1.5rem;
      margin-bottom: 2rem;
      position: relative;

      &:not(:last-child)::after {
        content: '';
        position: absolute;
        left: -20px;
        top: 50px;
        width: 2px;
        height: 40px;
        background: linear-gradient(to bottom, #2e7d32, transparent);
      }

      &:last-child::after {
        display: none;
      }
    }

    .timeline-marker {
      width: 40px;
      height: 40px;
      border-radius: 50%;
      background-color: rgba(46, 125, 50, 0.2);
      display: flex;
      align-items: center;
      justify-content: center;
      color: #2e7d32;
      position: absolute;
      left: -30px;
      top: 0;
      flex-shrink: 0;

      mat-icon {
        font-size: 20px;
        width: 20px;
        height: 20px;
      }

      &.success {
        background-color: rgba(76, 175, 80, 0.2);
        color: #4caf50;
      }
    }

    .timeline-content {
      flex: 1;
    }

    .timeline-title {
      color: #c8e6c9;
      font-weight: 600;
      font-size: 1rem;
    }

    .timeline-date {
      color: #999999;
      font-size: 0.85rem;
      margin-top: 0.25rem;
    }

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    @media (max-width: 768px) {
      .detail-container {
        padding: 1rem;
      }

      .detail-header {
        flex-direction: column;
        align-items: flex-start;
      }

      .detail-grid,
      .result-grid {
        grid-template-columns: 1fr;
      }

      .actions-section {
        flex-direction: column;
      }

      button {
        width: 100%;
      }
    }
  `]
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
    private router: Router
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

    this.bookingService.getBooking(id).subscribe({
      next: (booking) => {
        this.booking = booking;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  retryBooking(): void {
    if (!this.booking?.id) return;

    this.retrying = true;
    this.bookingService.retryBooking(this.booking.id).subscribe({
      next: (updatedBooking) => {
        this.booking = updatedBooking;
        this.retrying = false;
      },
      error: () => {
        this.retrying = false;
      }
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
