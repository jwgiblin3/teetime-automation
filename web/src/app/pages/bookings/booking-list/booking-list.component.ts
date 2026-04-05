import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { FormsModule } from '@angular/forms';
import { BookingService } from '../../../services/booking.service';
import { finalize } from 'rxjs/operators';
import { BookingRequest, BookingStatus, getStatusLabel } from '../../../models/booking.models';
import { StatusChipComponent } from '../../../shared/status-chip/status-chip.component';

@Component({
  selector: 'app-booking-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatSelectModule,
    MatFormFieldModule,
    MatProgressSpinnerModule,
    StatusChipComponent
  ],
  template: `
    <div class="bookings-container">
      <div class="bookings-header">
        <h1>My Bookings</h1>
        <button mat-raised-button color="primary" routerLink="/bookings/new">
          <mat-icon>add</mat-icon>
          New Booking
        </button>
      </div>

      <mat-card class="filters-card">
        <mat-form-field class="filter-field">
          <mat-label>Filter by Status</mat-label>
          <mat-select [(ngModel)]="selectedStatus" (selectionChange)="onStatusChange()">
            <mat-option value="">All Statuses</mat-option>
            <mat-option value="pending">Pending</mat-option>
            <mat-option value="scheduled">Scheduled</mat-option>
            <mat-option value="in-progress">In Progress</mat-option>
            <mat-option value="booked">Booked</mat-option>
            <mat-option value="failed">Failed</mat-option>
            <mat-option value="cancelled">Cancelled</mat-option>
          </mat-select>
        </mat-form-field>
      </mat-card>

      <div *ngIf="loading" class="loading-spinner">
        <mat-spinner diameter="50"></mat-spinner>
      </div>

      <div *ngIf="!loading && bookings.length === 0" class="empty-state">
        <mat-icon>golf_course</mat-icon>
        <p>No bookings found</p>
        <button mat-raised-button color="primary" routerLink="/bookings/new">
          Create Your First Booking
        </button>
      </div>

      <mat-card *ngIf="!loading && bookings.length > 0" class="bookings-card">
        <table mat-table [dataSource]="bookings" class="bookings-table">
          <!-- Course Column -->
          <ng-container matColumnDef="course">
            <th mat-header-cell *matHeaderCellDef>Course</th>
            <td mat-cell *matCellDef="let element">{{ element.courseName }}</td>
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
              <button
                mat-icon-button
                (click)="cancelBooking(element.id)"
                matTooltip="Cancel Booking"
                [disabled]="element.status === 'booked'"
              >
                <mat-icon>close</mat-icon>
              </button>
            </td>
          </ng-container>

          <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
          <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
        </table>
      </mat-card>
    </div>
  `,
  styles: [`
    .bookings-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .bookings-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    .filters-card {
      background-color: #1e1e1e !important;
      margin-bottom: 2rem;
      padding: 1.5rem;
    }

    .filter-field {
      width: 250px;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .bookings-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      overflow-x: auto;
    }

    .bookings-table {
      width: 100%;
      min-width: 600px;

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

    @media (max-width: 768px) {
      .bookings-container {
        padding: 1rem;
      }

      .bookings-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 1rem;
      }

      .filter-field {
        width: 100%;
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
export class BookingListComponent implements OnInit {
  bookings: BookingRequest[] = [];
  loading = false;
  selectedStatus = '';
  displayedColumns = ['course', 'date', 'time', 'players', 'status', 'actions'];

  constructor(
    private bookingService: BookingService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.loading = true;
    const status = this.selectedStatus as BookingStatus | '';

    this.bookingService.getBookings(status || undefined).pipe(
      finalize(() => { this.loading = false; this.cdr.detectChanges(); })
    ).subscribe({
      next: (bookings) => { this.bookings = bookings; },
      error: () => {}
    });
  }

  onStatusChange(): void {
    this.loadBookings();
  }

  cancelBooking(id: string): void {
    if (confirm('Are you sure you want to cancel this booking?')) {
      this.bookingService.cancelBooking(id).subscribe({
        next: () => {
          this.loadBookings();
        }
      });
    }
  }
}
