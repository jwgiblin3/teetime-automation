import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="login-container">
      <mat-card class="login-card">
        <mat-card-header>
          <div class="header-icon">
            <mat-icon>golf_course</mat-icon>
          </div>
          <mat-card-title>TeeTime Automator</mat-card-title>
          <mat-card-subtitle>Sign in to your account</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="loginForm" (ngSubmit)="onSubmit()">
            <mat-form-field class="full-width" appearance="outline">
              <mat-label>Email</mat-label>
              <input
                matInput
                type="email"
                formControlName="email"
                placeholder="your@email.com"
              />
              <mat-error *ngIf="getControl('email')?.hasError('required')">
                Email is required
              </mat-error>
              <mat-error *ngIf="getControl('email')?.hasError('email')">
                Please enter a valid email
              </mat-error>
            </mat-form-field>

            <mat-form-field class="full-width" appearance="outline">
              <mat-label>Password</mat-label>
              <input
                matInput
                [type]="hidePassword ? 'password' : 'text'"
                formControlName="password"
                placeholder="Enter your password"
              />
              <button
                mat-icon-button
                matSuffix
                (click)="hidePassword = !hidePassword"
                type="button"
              >
                <mat-icon>
                  {{ hidePassword ? 'visibility_off' : 'visibility' }}
                </mat-icon>
              </button>
              <mat-error *ngIf="getControl('password')?.hasError('required')">
                Password is required
              </mat-error>
            </mat-form-field>

            <div class="button-group">
              <button
                mat-raised-button
                color="primary"
                type="submit"
                [disabled]="loading || loginForm.invalid"
              >
                <mat-icon *ngIf="!loading">login</mat-icon>
                <mat-spinner
                  *ngIf="loading"
                  diameter="20"
                  class="inline-spinner"
                ></mat-spinner>
                {{ loading ? 'Signing in...' : 'Sign In' }}
              </button>

              <button
                mat-stroked-button
                type="button"
                class="google-btn"
                (click)="loginWithGoogle()"
                [disabled]="loading"
              >
                <svg
                  width="18"
                  height="18"
                  viewBox="0 0 24 24"
                  fill="currentColor"
                >
                  <path
                    d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"
                    fill="#1f2937"
                  />
                  <path
                    d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"
                    fill="#34a853"
                  />
                  <path
                    d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"
                    fill="#fbbc05"
                  />
                  <path
                    d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"
                    fill="#ea4335"
                  />
                </svg>
                Sign in with Google
              </button>
            </div>
          </form>

          <div class="divider">or</div>

          <div class="register-link">
            Don't have an account?
            <a routerLink="/register" class="link">Create one now</a>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .login-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 64px);
      background: linear-gradient(135deg, #0d3817 0%, #1b5e20 100%);
      padding: 2rem;
    }

    .login-card {
      width: 100%;
      max-width: 400px;
      background-color: #1e1e1e !important;
      border-radius: 8px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
    }

    mat-card-header {
      display: flex;
      flex-direction: column;
      align-items: center;
      margin-bottom: 2rem;
      text-align: center;
    }

    .header-icon {
      margin-bottom: 1rem;

      mat-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: #2e7d32;
      }
    }

    mat-card-title {
      color: #c8e6c9 !important;
      font-size: 1.75rem;
      margin-bottom: 0.5rem;
    }

    mat-card-subtitle {
      color: #999999 !important;
      font-size: 0.95rem;
    }

    form {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .full-width {
      width: 100%;
    }

    .button-group {
      display: flex;
      flex-direction: column;
      gap: 1rem;
      margin-top: 1rem;
    }

    button[type="submit"] {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      height: 44px;
    }

    .google-btn {
      background-color: #ffffff !important;
      color: #1f2937 !important;
      border: 1px solid #e5e7eb !important;
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.75rem;
      text-transform: none;
      font-weight: 500;
    }

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    .divider {
      text-align: center;
      color: #666666;
      margin: 1.5rem 0;
      position: relative;

      &::before,
      &::after {
        content: '';
        position: absolute;
        top: 50%;
        width: 45%;
        height: 1px;
        background-color: #333333;
      }

      &::before {
        left: 0;
      }

      &::after {
        right: 0;
      }
    }

    .register-link {
      text-align: center;
      color: #999999;
      font-size: 0.9rem;

      .link {
        color: #43a047;
        text-decoration: none;
        font-weight: 600;
        cursor: pointer;

        &:hover {
          text-decoration: underline;
        }
      }
    }

    @media (max-width: 480px) {
      .login-container {
        padding: 1rem;
      }

      .login-card {
        max-width: 100%;
      }
    }

    /* ── Input field branding ── */
    ::ng-deep .login-card {
      /* Input text */
      .mat-mdc-input-element {
        color: #c8e6c9 !important;
        font-size: 1.75rem !important;
        line-height: 1.2 !important;
      }

      /* Floating label (unfocused + focused) */
      .mat-mdc-floating-label,
      .mdc-floating-label {
        color: #c8e6c9 !important;
      }

      /* Input background */
      .mdc-text-field {
        background-color: #121212 !important;
        border-radius: 4px 4px 0 0;
      }

      /* Outline border */
      .mdc-notched-outline__leading,
      .mdc-notched-outline__notch,
      .mdc-notched-outline__trailing {
        border-color: #2e7d32 !important;
      }

      /* Focused outline */
      .mdc-text-field--focused .mdc-notched-outline__leading,
      .mdc-text-field--focused .mdc-notched-outline__notch,
      .mdc-text-field--focused .mdc-notched-outline__trailing {
        border-color: #c8e6c9 !important;
      }

      /* Visibility toggle icon */
      .mat-mdc-icon-button .mat-icon {
        color: #c8e6c9 !important;
      }
            /* Chrome autofill override */
                  input:-webkit-autofill,
                        input:-webkit-autofill:hover,
                              input:-webkit-autofill:focus,
                                    input:-webkit-autofill:active {

                                            -webkit-box-shadow: 0 0 0 1000px #121212 inset !important;
                                                    -webkit-text-fill-color: #c8e6c9 !important;
                                                            caret-color: #c8e6c9;
                                                                    transition: background-color 9999s ease-in-out 0s;
    }
    }
  `]
})
export class LoginComponent implements OnInit {
  loginForm!: FormGroup;
  loading = false;
  hidePassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm(): void {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      return;
    }

    this.loading = true;
    const { email, password } = this.loginForm.value;

    this.authService.login(email, password).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  loginWithGoogle(): void {
    this.authService.loginWithGoogle();
  }

  getControl(name: string) {
    return this.loginForm.get(name);
  }
}
