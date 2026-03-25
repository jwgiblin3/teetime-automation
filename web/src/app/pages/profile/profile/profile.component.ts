import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
  AbstractControl,
  ValidationErrors,
  ValidatorFn
} from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { AuthService } from '../../../services/auth.service';
import { CourseService } from '../../../services/course.service';
import { User } from '../../../models/auth.models';
import { Course } from '../../../models/course.models';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatTabsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSnackBarModule
  ],
  template: `
    <div class="profile-container">
      <div class="profile-header">
        <h1>My Profile</h1>
      </div>

      <mat-tab-group>
        <!-- Personal Info Tab -->
        <mat-tab label="Personal Information">
          <mat-card class="profile-card">
            <mat-card-header>
              <mat-card-title>Edit Profile</mat-card-title>
            </mat-card-header>

            <mat-card-content *ngIf="currentUser">
              <form [formGroup]="profileForm" (ngSubmit)="updateProfile()">
                <div class="form-row">
                  <mat-form-field class="half-width">
                    <mat-label>First Name</mat-label>
                    <input
                      matInput
                      formControlName="firstName"
                      placeholder="John"
                    />
                    <mat-error *ngIf="profileForm.get('firstName')?.hasError('required')">
                      First name is required
                    </mat-error>
                  </mat-form-field>

                  <mat-form-field class="half-width">
                    <mat-label>Last Name</mat-label>
                    <input
                      matInput
                      formControlName="lastName"
                      placeholder="Doe"
                    />
                    <mat-error *ngIf="profileForm.get('lastName')?.hasError('required')">
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
                    [attr.disabled]="'disabled'"
                  />
                  <mat-hint>Email cannot be changed</mat-hint>
                </mat-form-field>

                <mat-form-field class="full-width">
                  <mat-label>Phone Number</mat-label>
                  <input
                    matInput
                    type="tel"
                    formControlName="phone"
                    placeholder="(123) 456-7890"
                  />
                  <mat-error *ngIf="profileForm.get('phone')?.hasError('required')">
                    Phone number is required
                  </mat-error>
                </mat-form-field>

                <div class="button-group">
                  <button
                    mat-raised-button
                    color="primary"
                    type="submit"
                    [disabled]="updatingProfile || profileForm.invalid"
                  >
                    <mat-spinner
                      *ngIf="updatingProfile"
                      diameter="20"
                      class="inline-spinner"
                    ></mat-spinner>
                    {{ updatingProfile ? 'Saving...' : 'Save Changes' }}
                  </button>
                </div>
              </form>
            </mat-card-content>
          </mat-card>
        </mat-tab>

        <!-- Change Password Tab -->
        <mat-tab label="Security">
          <mat-card class="profile-card">
            <mat-card-header>
              <mat-card-title>Change Password</mat-card-title>
            </mat-card-header>

            <mat-card-content>
              <form [formGroup]="passwordForm" (ngSubmit)="changePassword()">
                <mat-form-field class="full-width">
                  <mat-label>Current Password</mat-label>
                  <input
                    matInput
                    [type]="hideCurrentPassword ? 'password' : 'text'"
                    formControlName="currentPassword"
                  />
                  <button
                    mat-icon-button
                    matSuffix
                    (click)="hideCurrentPassword = !hideCurrentPassword"
                    type="button"
                  >
                    <mat-icon>
                      {{ hideCurrentPassword ? 'visibility_off' : 'visibility' }}
                    </mat-icon>
                  </button>
                  <mat-error *ngIf="passwordForm.get('currentPassword')?.hasError('required')">
                    Current password is required
                  </mat-error>
                </mat-form-field>

                <mat-form-field class="full-width">
                  <mat-label>New Password</mat-label>
                  <input
                    matInput
                    [type]="hideNewPassword ? 'password' : 'text'"
                    formControlName="newPassword"
                  />
                  <button
                    mat-icon-button
                    matSuffix
                    (click)="hideNewPassword = !hideNewPassword"
                    type="button"
                  >
                    <mat-icon>
                      {{ hideNewPassword ? 'visibility_off' : 'visibility' }}
                    </mat-icon>
                  </button>
                  <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('required')">
                    New password is required
                  </mat-error>
                  <mat-error *ngIf="passwordForm.get('newPassword')?.hasError('minlength')">
                    Password must be at least 8 characters
                  </mat-error>
                </mat-form-field>

                <mat-form-field class="full-width">
                  <mat-label>Confirm New Password</mat-label>
                  <input
                    matInput
                    [type]="hideConfirmPassword ? 'password' : 'text'"
                    formControlName="confirmPassword"
                  />
                  <button
                    mat-icon-button
                    matSuffix
                    (click)="hideConfirmPassword = !hideConfirmPassword"
                    type="button"
                  >
                    <mat-icon>
                      {{ hideConfirmPassword ? 'visibility_off' : 'visibility' }}
                    </mat-icon>
                  </button>
                  <mat-error *ngIf="passwordForm.hasError('passwordMismatch')">
                    Passwords do not match
                  </mat-error>
                </mat-form-field>

                <div class="button-group">
                  <button
                    mat-raised-button
                    color="primary"
                    type="submit"
                    [disabled]="changingPassword || passwordForm.invalid"
                  >
                    <mat-spinner
                      *ngIf="changingPassword"
                      diameter="20"
                      class="inline-spinner"
                    ></mat-spinner>
                    {{ changingPassword ? 'Updating...' : 'Change Password' }}
                  </button>
                </div>
              </form>
            </mat-card-content>
          </mat-card>
        </mat-tab>

        <!-- Connected Courses Tab -->
        <mat-tab label="Connected Courses">
          <mat-card class="profile-card">
            <mat-card-header>
              <mat-card-title>Your Connected Courses</mat-card-title>
            </mat-card-header>

            <mat-card-content>
              <div *ngIf="loadingCourses" class="loading-spinner">
                <mat-spinner diameter="40"></mat-spinner>
              </div>

              <div *ngIf="!loadingCourses && courses.length === 0" class="empty-state">
                <mat-icon>golf_course</mat-icon>
                <p>No courses configured yet</p>
              </div>

              <div *ngIf="!loadingCourses && courses.length > 0" class="courses-list">
                <div class="course-item" *ngFor="let course of courses">
                  <div class="course-name">{{ course.name }}</div>
                  <div class="course-details">
                    <span class="platform">{{ getPlatformLabel(course.platform) }}</span>
                    <span class="status" [class.configured]="course.credentialsSaved">
                      {{ course.credentialsSaved ? '✓ Configured' : '⚠ Not Configured' }}
                    </span>
                  </div>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .profile-container {
      padding: 2rem;
      max-width: 900px;
      margin: 0 auto;
    }

    .profile-header {
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    mat-tab-group {
      background-color: transparent;
    }

    .profile-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      margin-top: 1.5rem;
    }

    mat-card-header {
      margin-bottom: 1.5rem;
    }

    mat-card-title {
      color: #c8e6c9 !important;
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
      gap: 1rem;
      margin-top: 1rem;
    }

    button[type="submit"] {
      width: 150px;
    }

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    .loading-spinner {
      display: flex;
      justify-content: center;
      padding: 2rem 0;
    }

    .courses-list {
      display: flex;
      flex-direction: column;
      gap: 1rem;
    }

    .course-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem;
      background-color: #252525;
      border-radius: 4px;
      border-left: 4px solid #2e7d32;
    }

    .course-name {
      font-weight: 600;
      color: #c8e6c9;
      font-size: 1.05rem;
    }

    .course-details {
      display: flex;
      gap: 1.5rem;
      align-items: center;
    }

    .platform {
      background-color: #2e7d32;
      color: #ffffff;
      padding: 0.25rem 0.75rem;
      border-radius: 12px;
      font-size: 0.85rem;
      font-weight: 500;
    }

    .status {
      color: #ff9800;
      font-size: 0.9rem;

      &.configured {
        color: #4caf50;
      }
    }

    @media (max-width: 768px) {
      .profile-container {
        padding: 1rem;
      }

      .form-row {
        grid-template-columns: 1fr;
      }

      .course-item {
        flex-direction: column;
        align-items: flex-start;
        gap: 0.5rem;
      }

      .course-details {
        flex-wrap: wrap;
        width: 100%;
      }

      button[type="submit"] {
        width: 100%;
      }
    }
  `]
})
export class ProfileComponent implements OnInit {
  profileForm!: FormGroup;
  passwordForm!: FormGroup;

  currentUser: User | null = null;
  courses: Course[] = [];

  updatingProfile = false;
  changingPassword = false;
  loadingCourses = false;

  hideCurrentPassword = true;
  hideNewPassword = true;
  hideConfirmPassword = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private courseService: CourseService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.currentUser = this.authService.getCurrentUser();
    this.initializeForms();
    this.loadCourses();
  }

  initializeForms(): void {
    this.profileForm = this.fb.group({
      firstName: [this.currentUser?.firstName || '', Validators.required],
      lastName: [this.currentUser?.lastName || '', Validators.required],
      email: [this.currentUser?.email || '', Validators.required],
      phone: [this.currentUser?.phoneNumber || '', Validators.required]
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(8)]],
        confirmPassword: ['', Validators.required]
      },
      { validators: this.passwordMatchValidator }
    );
  }

  passwordMatchValidator: ValidatorFn = (
    form: AbstractControl
  ): ValidationErrors | null => {
    const newPassword = form.get('newPassword');
    const confirmPassword = form.get('confirmPassword');

    if (!newPassword || !confirmPassword) {
      return null;
    }

    return newPassword.value === confirmPassword.value
      ? null
      : { passwordMismatch: true };
  };

  updateProfile(): void {
    if (this.profileForm.invalid) {
      return;
    }

    this.updatingProfile = true;
    // In a real app, you would call an API to update the profile
    setTimeout(() => {
      this.updatingProfile = false;
      this.snackBar.open('Profile updated successfully!', 'Close', {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'bottom'
      });
    }, 1000);
  }

  changePassword(): void {
    if (this.passwordForm.invalid) {
      return;
    }

    this.changingPassword = true;
    // In a real app, you would call an API to change the password
    setTimeout(() => {
      this.changingPassword = false;
      this.passwordForm.reset();
      this.snackBar.open('Password changed successfully!', 'Close', {
        duration: 3000,
        horizontalPosition: 'end',
        verticalPosition: 'bottom'
      });
    }, 1000);
  }

  loadCourses(): void {
    this.loadingCourses = true;
    this.courseService.getCourses().subscribe({
      next: (courses) => {
        this.courses = courses;
        this.loadingCourses = false;
      },
      error: () => {
        this.loadingCourses = false;
      }
    });
  }

  getPlatformLabel(platform: string): string {
    const labels: Record<string, string> = {
      'cps-golf': 'CPS Golf',
      'golfnow': 'GolfNow',
      'teesnap': 'TeeSnap',
      'foreup': 'ForeUp',
      'other': 'Other'
    };
    return labels[platform] || platform;
  }
}
