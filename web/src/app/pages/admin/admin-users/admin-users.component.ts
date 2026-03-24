import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { AdminService } from '../../../services/admin.service';
import { AdminUser } from '../../../models/admin.models';

@Component({
  selector: 'app-admin-users',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  template: `
    <div class="admin-users-container">
      <div class="page-header">
        <h1>Manage Users</h1>
      </div>

      <mat-card class="users-card">
        <div *ngIf="loading" class="loading-spinner">
          <mat-spinner diameter="50"></mat-spinner>
        </div>

        <div *ngIf="!loading">
          <table mat-table [dataSource]="users" class="users-table">
            <!-- Name Column -->
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Name</th>
              <td mat-cell *matCellDef="let element">
                {{ element.firstName }} {{ element.lastName }}
              </td>
            </ng-container>

            <!-- Email Column -->
            <ng-container matColumnDef="email">
              <th mat-header-cell *matHeaderCellDef>Email</th>
              <td mat-cell *matCellDef="let element">
                {{ element.email }}
              </td>
            </ng-container>

            <!-- Phone Column -->
            <ng-container matColumnDef="phone">
              <th mat-header-cell *matHeaderCellDef>Phone</th>
              <td mat-cell *matCellDef="let element">
                {{ element.phone }}
              </td>
            </ng-container>

            <!-- Bookings Column -->
            <ng-container matColumnDef="bookings">
              <th mat-header-cell *matHeaderCellDef>Bookings</th>
              <td mat-cell *matCellDef="let element">
                {{ element.bookingCount }}
              </td>
            </ng-container>

            <!-- Status Column -->
            <ng-container matColumnDef="status">
              <th mat-header-cell *matHeaderCellDef>Status</th>
              <td mat-cell *matCellDef="let element">
                <span class="status-badge" [class.disabled]="element.isDisabled">
                  {{ element.isDisabled ? 'Disabled' : 'Active' }}
                </span>
              </td>
            </ng-container>

            <!-- Actions Column -->
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let element">
                <button
                  mat-icon-button
                  (click)="toggleUserStatus(element)"
                  [matTooltip]="element.isDisabled ? 'Enable User' : 'Disable User'"
                >
                  <mat-icon>
                    {{ element.isDisabled ? 'check_circle' : 'block' }}
                  </mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns;"></tr>
          </table>

          <mat-paginator
            [length]="totalUsers"
            [pageSize]="pageSize"
            [pageSizeOptions]="[10, 20, 50]"
            (page)="onPageChange($event)"
          ></mat-paginator>
        </div>
      </mat-card>
    </div>
  `,
  styles: [`
    .admin-users-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    .users-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      overflow: hidden;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 3rem 0;
    }

    .users-table {
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

    .status-badge {
      background-color: #4caf50;
      color: #ffffff;
      padding: 0.25rem 0.75rem;
      border-radius: 12px;
      font-size: 0.85rem;
      font-weight: 500;

      &.disabled {
        background-color: #d32f2f;
      }
    }

    @media (max-width: 768px) {
      .admin-users-container {
        padding: 1rem;
      }

      .users-table {
        font-size: 0.85rem;

        th, td {
          padding: 0.75rem;
        }
      }
    }
  `]
})
export class AdminUsersComponent implements OnInit {
  users: AdminUser[] = [];
  loading = false;
  totalUsers = 0;
  pageSize = 20;
  currentPage = 0;

  displayedColumns = ['name', 'email', 'phone', 'bookings', 'status', 'actions'];

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.adminService.getUsers(this.currentPage + 1, this.pageSize).subscribe({
      next: (response) => {
        this.users = response.users;
        this.totalUsers = response.total;
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
    this.loadUsers();
  }

  toggleUserStatus(user: AdminUser): void {
    if (user.isDisabled) {
      this.adminService.enableUser(user.id).subscribe({
        next: () => {
          this.loadUsers();
        }
      });
    } else {
      this.adminService.disableUser(user.id).subscribe({
        next: () => {
          this.loadUsers();
        }
      });
    }
  }
}

import { MatTooltipModule } from '@angular/material/tooltip';
