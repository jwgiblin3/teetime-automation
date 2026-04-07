import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { BookingStatus, getStatusIcon, getStatusLabel, getStatusColor } from '../../models/booking.models';

@Component({
  selector: 'app-status-chip',
  standalone: true,
  imports: [CommonModule, MatChipsModule, MatIconModule],
  templateUrl: './status-chip.component.html',
  styleUrls: ['./status-chip.component.css']
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
