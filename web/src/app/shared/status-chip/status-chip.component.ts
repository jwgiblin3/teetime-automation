import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { BookingStatus, getStatusIcon, getStatusLabel, getStatusColor } from '../../models/booking.models';

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatIconModule],
  template: `
    <mat-chip
      [class]="'status-' + status"
      class="status-chip"
    >
      <mat-icon class="status-icon">{{ getStatusIcon(status) }}</mat-icon>
      <span>{{ getStatusLabel(status) }}</span>
    </mat-chip>
  `,
  styles: [`
    .status-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem !important;
      font-weight: 500;
    }

    .status-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      margin: 0;
    }

    .status-pending {
      background-color: #757575 !important;
      color: #ffffff !important;
    }

    .status-scheduled {
      background-color: #2196f3 !important;
      color: #ffffff !important;
    }

    .status-in-progress {
      background-color: #ff9800 !important;
      color: #ffffff !important;
    }

    .status-booked {
      background-color: #4caf50 !important;
      color: #ffffff !important;
    }

    .status-failed {
      background-color: #d32f2f !important;
      color: #ffffff !important;
    }

    .status-cancelled {
      background-color: #616161 !important;
      color: #ffffff !important;
    }
  `]
})
export class StatusChipComponent {
  @Input() status!: BookingStatus;

  getStatusIcon(status: BookingStatus): string {
    return getStatusIcon(status);
  }

  getStatusLabel(status: BookingStatus): string {
    return getStatusLabel(status);
  }
}
