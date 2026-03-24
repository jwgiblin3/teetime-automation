import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { AdminService } from '../../../services/admin.service';
import { AdminBooking, AdminBooking as AdminBookingModel } from '../../../models/admin.models';
import { StatusChipComponent } from '../../../shared/status-chip/status-chip.component';

@Component({
  selector: 'app-admin-bookings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule,
    StatusChipComponent
  ],
  template: `
    <div class="admin-bookings-container">
      <div class="page-header">
        <h1>All Bookings</h1>
      </div>

      <mat-card class="bookings-card">
        <div *ngIf="loading" class="loading-spinner">
          <mat-spinner diameter="50"></mat-spinner>
        </div>

        <div *ngIf="!loading">
          <table mat-table [dataSource]="bookings" class="bookings-table">
            <!-- User Column -->
            <ng-container matColumnDef="user">
              <th mat-header-cell *matHeaderCellDef>User</th>
              <td mat-cell *matCellDef="let element">
                <div class="user-info">
                  <div>{{ element.userName }}</div>
                  <div class="user-email">{{ element.userEmail }}</div>
                </div>
              </td>
            </ng-container>

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

            <!-- Created Column -->
            <ng-container matColumnDef="created">
              <th mat-header-cell *matHeaderCellDef>Created</th>
              <td mat-cell *matCellDef="let element">
                {{ element.createdAt | date: 'MMM dd, HH:mm' }}
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let element">
                <button
                  mat-icon-button
                  matTooltip="View Details"
                >
                  <mat-icon>visibility</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator
            [length]="totalBookings"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPageChange($event)"
          ></mat-paginator>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .admin-bookings-container {
      padding: 2rem;
      max-width: 1400px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    .bookings-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      overflow: hidden;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
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

    .user-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .user-email {
      color: #999999;
      font-size: 0.85rem;
    }

    @media (max-width: 1024px) {
      .admin-bookings-container {
        padding: 1rem;
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
export class AdminBookingsComponent implements OnInit {
  bookings: AdminBookingModel[] = [];
  loading = false;
  totalBookings = 0;
  pageSize = 20;
  currentPage = 0;

  displayedColumns = ['user', 'course', 'date', 'time', 'players', 'status', 'created', 'actions'];

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadBookings();
  }

  loadBookings(): void {
    this.loading = true;
    this.adminService.getBookings(this.currentPage + 1, this.pageSize).subscribe({
      next: (response) => {
        this.bookings = response.bookings;
        this.totalBookings = response.total;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.currentPage = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadBookings();
  }
}

import { MatTooltipModule } from '@angular/material/tooltip';
