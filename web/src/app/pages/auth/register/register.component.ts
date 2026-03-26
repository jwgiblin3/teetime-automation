import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
  ValidatorFn
} from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../services/auth.service';
import { RegisterRequest } from '../../../models/auth.models';

@Component({
  selector: 'app-register',
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
    <div class="register-container">
      <mat-card class="register-card">
        <mat-card-header>
          <div class="header-icon">
            <mat-icon>golf_course</mat-icon>
          </div>
          <mat-card-title>Create Account</mat-card-title>
          <mat-card-subtitle>Join TeeTime Automator</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <form [formGroup]="registerForm" (ngSubmit)="onSubmit()">
            <div class="form-row">
              <mat-form-field class="half-width">
                <mat-label>First Name</mat-label>
                <input
                  matInput
                  type="text"
                  formControlName="firstName"
                  placeholder="John"
                />
                <mat-error *ngIf="getControl('firstName')?.hasError('required')">
                  First name is required
                </mat-error>
              </mat-form-field>

              <mat-form-field class="half-width">
                <mat-label>Last Name</mat-label>
                <input
                  matInput
                  type="text"
                  formControlName="lastName"
                  placeholder="Doe"
                />
                <mat-error *ngIf="getControl('lastName')?.hasError('required')">
                  Last name is required
                </mat-error>
              </mat-form-field>
            </div>

            <mat-form-field class="full-width">
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

            <mat-form-field class="full-width">
              <mat-label>Phone Number</mat-label>
              <input
                matInput
                type="tel"
                formControlName="phone"
                placeholder="(123) 456-7890"
              />
              <mat-error *ngIf="getControl('phone')?.hasError('required')">
                Phone number is required
              </mat-error>
            </mat-form-field>

            <mat-form-field class="full-width">
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
              <mat-error *ngIf="getControl('password')?.hasError('minlength')">
                Password must be at least 8 characters
              </mat-error>
            </mat-form-field>

            <mat-form-field class="full-width">
              <mat-label>Confirm Password</mat-label>
              <input
                matInput
                [type]="hideConfirm ? 'password' : 'text'"
                formControlName="confirmPassword"
                placeholder="Confirm your password"
              />
              <button
                mat-icon-button
                matSuffix
                (click)="hideConfirm = !hideConfirm"
                type="button"
              >
                <mat-icon>
                  {{ hideConfirm ? 'visibility_off' : 'visibility' }}
                </mat-icon>
              </button>
              <mat-error *ngIf="getControl('confirmPassword')?.hasError('required')">
                Please confirm your password
              </mat-error>
              <mat-error *ngIf="registerForm.hasError('passwordMismatch')">
                Passwords do not match
              </mat-error>
            </mat-form-field>

            <div class="button-group">
              <button
                mat-raised-button
                color="primary"
                type="submit"
                [disabled]="loading || registerForm.invalid"
              >
                <mat-icon *ngIf="!loading">person_add</mat-icon>
                <mat-spinner
                  *ngIf="loading"
                  diameter="20"
                  class="inline-spinner"
                ></mat-spinner>
                {{ loading ? 'Creating Account...' : 'Create Account' }}
              </button>
            </div>
          </form>

          <div class="login-link">
            Already have an account?
            <a routerLink="/login" class="link">Sign in here</a>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .register-container {
      display: flex;
      justify-content: center;
      align-items: center;
      min-height: calc(100vh - 64px);
      background: linear-gradient(135deg, #0d3817 0%, #1b5e20 100%);
      padding: 2rem 1rem;
    }

    .register-card {
      width: 100%;
      max-width: 500px;
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

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .full-width {
      width: 100%;
    }

    .half-width {
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

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    .login-link {
      text-align: center;
      color: #999999;
      font-size: 0.9rem;
      margin-top: 1.5rem;

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

    @media (max-width: 600px) {
      .register-container {
        padding: 1rem;
      }

      .register-card {
        max-width: 100%;
      }

      .form-row {
        grid-template-columns: 1fr;
      }
    }
  `]
})
export class RegisterComponent implements OnInit {
  registerForm!: FormGroup;
  loading = false;
  hidePassword = true;
  hideConfirm = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForm();
  }

  initializeForm(): void {
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required]],
        lastName: ['', [Validators.required]],
        email: ['', [Validators.required, Validators.email]],
        phone: ['', [Validators.required]],
        password: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', [Validators.required]]
      },
      { validators: this.passwordMatchValidator }
    );
  }

  passwordMatchValidator: ValidatorFn = (
    form: AbstractControl
  ): ValidationErrors | null => {
    const password = form.get('password');
    const confirmPassword = form.get('confirmPassword');

    if (!password || !confirmPassword) {
      return null;
    }

    return password.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  };

  onSubmit(): void {
    if (this.registerForm.invalid) {
      return;
    }

    this.loading = true;
    const request: RegisterRequest = {
      firstName: this.registerForm.value.firstName,
      lastName: this.registerForm.value.lastName,
      email: this.registerForm.value.email,
      phoneNumber: this.registerForm.value.phone,
      password: this.registerForm.value.password,
      confirmPassword: this.registerForm.value.confirmPassword
    };

    this.authService.register(request).subscribe({
      next: () => {
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  getControl(name: string) {
    return this.registerForm.get(name);
  }
}
