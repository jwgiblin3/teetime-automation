import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatDividerModule } from '@angular/material/divider';
import { map } from 'rxjs/operators';
import { AuthService } from '../../services/auth.service';
import { User } from '../../models/auth.models';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    MatToolbarModule,
    MatButtonModule,
    MatMenuModule,
    MatIconModule,
    MatDividerModule
  ],
  template: `
    <mat-toolbar color="primary" class="navbar">
      <div class="navbar-content">
        <div class="navbar-brand">
          <mat-icon>golf_course</mat-icon>
          <span>TeeTime Automator</span>
        </div>

        <div class="navbar-links" *ngIf="currentUser$ | async as user">
          <a mat-button routerLink="/dashboard">
            <mat-icon>dashboard</mat-icon>
            Dashboard
          </a>
          <a mat-button routerLink="/bookings">
            <mat-icon>calendar_today</mat-icon>
            My Bookings
          </a>
          <a mat-button routerLink="/courses">
            <mat-icon>location_on</mat-icon>
            Courses
          </a>
        </div>

        <div class="navbar-actions" *ngIf="currentUser$ | async as user">
          <button
            mat-icon-button
            [matMenuTriggerFor]="userMenu"
            class="user-menu-trigger"
          >
            <mat-icon>account_circle</mat-icon>
            <span class="user-name">{{ user.firstName }}</span>
          </button>

          <mat-menu #userMenu="matMenu">
            <a mat-menu-item routerLink="/profile">
              <mat-icon>person</mat-icon>
              <span>Profile</span>
            </a>
            <a mat-menu-item routerLink="/admin" *ngIf="isAdmin$ | async">
              <mat-icon>admin_panel_settings</mat-icon>
              <span>Admin Dashboard</span>
            </a>
            <mat-divider *ngIf="isAdmin$ | async"></mat-divider>
            <button mat-menu-item (click)="logout()">
              <mat-icon>logout</mat-icon>
              <span>Logout</span>
            </button>
          </mat-menu>
        </div>

        <div class="navbar-actions" *ngIf="!(currentUser$ | async)">
          <a mat-raised-button routerLink="/login" class="login-btn">
            Login
          </a>
        </div>
      </div>
    </mat-toolbar>
  `,
  styles: [`
    .navbar {
      background: linear-gradient(135deg, #1b5e20 0%, #2e7d32 100%);
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.3);
      position: sticky;
      top: 0;
      z-index: 100;
    }

    .navbar-content {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0 1rem;
    }

    .navbar-brand {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 1.25rem;
      font-weight: 600;
      color: #ffffff;
      cursor: pointer;
      min-width: 200px;

      mat-icon {
        font-size: 28px;
        width: 28px;
        height: 28px;
      }
    }

    .navbar-links {
      display: flex;
      gap: 0.5rem;
      flex: 1;
      justify-content: center;

      a {
        color: #ffffff;
        text-transform: none;
        display: flex;
        align-items: center;
        gap: 0.5rem;

        mat-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
        }
      }
    }

    .navbar-actions {
      display: flex;
      align-items: center;
      gap: 1rem;
      min-width: 200px;
      justify-content: flex-end;
    }

    .user-menu-trigger {
      color: #ffffff !important;
      display: flex;
      align-items: center;
      gap: 0.5rem;

      .user-name {
        margin-left: 0.5rem;
        font-size: 0.9rem;
      }
    }

    .login-btn {
      background-color: #c8e6c9 !important;
      color: #1b5e20 !important;
      font-weight: 600;
    }

    @media (max-width: 768px) {
      .navbar-links {
        display: none;
      }

      .navbar-brand {
        min-width: auto;

        span {
          display: none;
        }
      }

      .user-name {
        display: none !important;
      }
    }
  `]
})
export class NavbarComponent implements OnInit {
  currentUser$ = this.authService.currentUser$;
  isAdmin$ = this.authService.currentUser$.pipe(
    map(user => user ? user.role === 'admin' : false)
  );

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {}

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
