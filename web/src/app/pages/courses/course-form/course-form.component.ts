import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CourseService } from '../../../services/course.service';
import {
  Course,
  CoursePlatform,
  CreateCourseRequest,
  getPlatformLabel
} from '../../../models/course.models';

@Component({
  selector: 'app-course-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  template: `
    <div class="form-container">
      <div class="page-header">
        <h1>{{ editMode ? 'Edit Course' : 'Add a New Course' }}</h1>
      </div>

      <mat-card class="stepper-card">
        <mat-stepper #stepper>
          <!-- Step 1: Basic Info -->
          <mat-step label="Course Info" [stepControl]="basicForm" editable>
            <form [formGroup]="basicForm" class="step-form">
              <mat-form-field class="full-width" appearance="outline">
                <mat-label>Course Name</mat-label>
                <input
                  matInput
                  formControlName="name"
                  placeholder="e.g., Pebble Beach Golf Links"
                />
                <mat-error *ngIf="basicForm.get('name')?.hasError('required')">
                  Course name is required
                </mat-error>
              </mat-form-field>

              <mat-form-field class="full-width" appearance="outline">
                <mat-label>Booking URL</mat-label>
                <input
                  matInput
                  type="url"
                  formControlName="bookingUrl"
                  placeholder="https://booking.example.com"
                />
                <mat-error *ngIf="basicForm.get('bookingUrl')?.hasError('required')">
                  Booking URL is required
                </mat-error>
              </mat-form-field>

              <mat-form-field class="full-width" appearance="outline">
                <mat-label>Booking Platform</mat-label>
                <mat-select formControlName="platform" required>
                  <mat-option [value]="CoursePlatform.CPS_GOLF">
                    {{ getPlatformLabel(CoursePlatform.CPS_GOLF) }}
                  </mat-option>
                  <mat-option [value]="CoursePlatform.GOLF_NOW">
                    {{ getPlatformLabel(CoursePlatform.GOLF_NOW) }}
                  </mat-option>
                  <mat-option [value]="CoursePlatform.TEE_SNAP">
                    {{ getPlatformLabel(CoursePlatform.TEE_SNAP) }}
                  </mat-option>
                  <mat-option [value]="CoursePlatform.FORE_UP">
                    {{ getPlatformLabel(CoursePlatform.FORE_UP) }}
                  </mat-option>
                  <mat-option [value]="CoursePlatform.OTHER">
                    {{ getPlatformLabel(CoursePlatform.OTHER) }}
                  </mat-option>
                </mat-select>
                <mat-error *ngIf="basicForm.get('platform')?.hasError('required')">
                  Please select a platform
                </mat-error>
              </mat-form-field>

              <div class="step-actions">
                <button mat-raised-button color="primary" matStepperNext>
                  Continue
                </button>
              </div>
            </form>
          </mat-step>

          <!-- Step 2: Release Schedule -->
          <mat-step label="Release Schedule" [stepControl]="scheduleForm" editable>
            <form [formGroup]="scheduleForm" class="step-form">
              <div class="schedule-section">
                <label class="section-label">Tee Time Release Schedule</label>
                <p class="section-description">
                  When does this course release new tee times?
                </p>

                <mat-form-field class="full-width" appearance="outline">
                  <mat-label>Days Before Release</mat-label>
                  <input
                    matInput
                    type="number"
                    formControlName="daysBeforeRelease"
                    min="0"
                    max="365"
                    placeholder="e.g., 7"
                  />
                  <mat-hint>Number of days in advance tee times are released</mat-hint>
                  <mat-error>Required (0-365)</mat-error>
                </mat-form-field>

                <div class="time-inputs">
                  <mat-form-field class="time-field" appearance="outline">
                    <mat-label>Release Hour</mat-label>
                    <input
                      matInput
                      type="number"
                      formControlName="releaseTimeHour"
                      min="0"
                      max="23"
                    />
                  </mat-form-field>

                  <mat-form-field class="time-field" appearance="outline">
                    <mat-label>Release Minute</mat-label>
                    <input
                      matInput
                      type="number"
                      formControlName="releaseTimeMinute"
                      min="0"
                      max="59"
                    />
                  </mat-form-field>
                </div>

                <p class="schedule-example">
                  Example: Release 7 days before at 8:00 AM means tee times open
                  every day at 8:00 AM local time
                </p>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="primary" matStepperNext>
                  Continue
                </button>
              </div>
            </form>
          </mat-step>

          <!-- Step 3: Credentials -->
          <mat-step label="Credentials" [stepControl]="credentialsForm" editable>
            <form [formGroup]="credentialsForm" class="step-form">
              <div class="credentials-section">
                <label class="section-label">Login Credentials</label>
                <p class="section-description">
                  Enter your login credentials for this golf course booking platform.
                  Your credentials are securely encrypted.
                </p>

                <mat-form-field class="full-width" appearance="outline">
                  <mat-label>Email</mat-label>
                  <input
                    matInput
                    type="email"
                    formControlName="email"
                    placeholder="your@email.com"
                    required
                  />
                  <mat-error *ngIf="credentialsForm.get('email')?.hasError('required')">
                    Email is required
                  </mat-error>
                  <mat-error *ngIf="credentialsForm.get('email')?.hasError('email')">
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
                    required
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
                  <mat-error *ngIf="credentialsForm.get('password')?.hasError('required')">
                    Password is required
                  </mat-error>
                </mat-form-field>

                <div class="security-note">
                  <mat-icon>info</mat-icon>
                  <span>
                    Your credentials are encrypted using AES-256 and stored securely
                    on our servers.
                  </span>
                </div>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="primary" matStepperNext>
                  Review
                </button>
              </div>
            </form>
          </mat-step>

          <!-- Step 4: Review -->
          <mat-step label="Review">
            <div class="review-section">
              <h3>Review Your Course Settings</h3>

              <div class="review-item">
                <span class="review-label">Course Name:</span>
                <span class="review-value">{{ basicForm.get('name')?.value }}</span>
              </div>

              <div class="review-item">
                <span class="review-label">Platform:</span>
                <span class="review-value">
                  {{ getPlatformLabel(basicForm.get('platform')?.value) }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Booking URL:</span>
                <span class="review-value url-value">
                  {{ basicForm.get('bookingUrl')?.value }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Release Schedule:</span>
                <span class="review-value">
                  {{ scheduleForm.get('daysBeforeRelease')?.value }} days before at
                  {{ formatTime(scheduleForm.get('releaseTimeHour')?.value, scheduleForm.get('releaseTimeMinute')?.value) }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Credentials:</span>
                <span class="review-value">
                  ✓ Email configured
                </span>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button
                  mat-raised-button
                  color="primary"
                  (click)="submitForm()"
                  [disabled]="submitting"
                >
                  <mat-spinner
                    *ngIf="submitting"
                    diameter="20"
                    class="inline-spinner"
                  ></mat-spinner>
                  {{ submitting ? 'Saving...' : 'Save Course' }}
                </button>
              </div>
            </div>
          </mat-step>
        </mat-stepper>
      </mat-card>
    </div>
  `,
  styles: [`
    .form-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .page-header {
      margin-bottom: 2rem;

      h1 {
        margin: 0;
      }
    }

    .stepper-card {
      background-color: #1e1e1e !important;
      border-radius: 8px;
      overflow: hidden;
    }

    .step-form {
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
      padding: 2rem;
    }

    .full-width {
      width: 100%;
    }

    .section-label {
      display: block;
      color: #c8e6c9;
      font-weight: 600;
      margin-bottom: 0.5rem;
      text-transform: uppercase;
      font-size: 0.85rem;
      letter-spacing: 0.5px;
    }

    .section-description {
      color: #999999;
      margin-bottom: 1.5rem;
      font-size: 0.95rem;
    }

    .schedule-section {
      margin-bottom: 1rem;
    }

    .time-inputs {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .time-field {
      width: 100%;
    }

    .schedule-example,
    .time-window-example {
      color: #999999;
      font-size: 0.9rem;
      font-style: italic;
      margin-top: 1rem;
    }

    .credentials-section {
      margin-bottom: 1rem;
    }

    .security-note {
      display: flex;
      align-items: flex-start;
      gap: 0.75rem;
      background-color: rgba(46, 125, 50, 0.1);
      border-left: 4px solid #2e7d32;
      padding: 1rem;
      border-radius: 4px;
      margin-top: 1rem;
      color: #a5d6a7;
      font-size: 0.9rem;

      mat-icon {
        flex-shrink: 0;
        margin-top: 2px;
      }
    }

    .step-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 2rem;
      padding-top: 1rem;
      border-top: 1px solid #303030;
    }

    .review-section {
      padding: 2rem;

      h3 {
        margin-top: 0;
        margin-bottom: 1.5rem;
      }
    }

    .review-item {
      display: flex;
      justify-content: space-between;
      padding: 1rem 0;
      border-bottom: 1px solid #303030;
      align-items: center;

      &:last-child {
        border-bottom: none;
      }
    }

    .review-label {
      color: #999999;
      font-weight: 600;
      text-transform: uppercase;
      font-size: 0.85rem;
      letter-spacing: 0.5px;
    }

    .review-value {
      color: #c8e6c9;
      font-size: 1rem;
      font-weight: 500;

      &.url-value {
        word-break: break-all;
      }
    }

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    @media (max-width: 768px) {
      .form-container {
        padding: 1rem;
      }

      .step-form {
        padding: 1.5rem;
      }

      .time-inputs {
        grid-template-columns: 1fr;
      }

      .step-actions {
        flex-direction: column-reverse;
      }

      button {
        width: 100%;
      }
    }
  `]
})
export class CourseFormComponent implements OnInit {
  basicForm!: FormGroup;
  scheduleForm!: FormGroup;
  credentialsForm!: FormGroup;

  editMode = false;
  submitting = false;
  hidePassword = true;
  courseId: string | null = null;

  CoursePlatform = CoursePlatform;

  constructor(
    private fb: FormBuilder,
    private courseService: CourseService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.courseId = this.route.snapshot.paramMap.get('id');
    this.editMode = !!this.courseId;
    this.initializeForms();

    if (this.editMode && this.courseId) {
      this.loadCourse(this.courseId);
    }
  }

  initializeForms(): void {
    this.basicForm = this.fb.group({
      name: ['', Validators.required],
      bookingUrl: ['', [Validators.required, Validators.pattern(/^https?:\/\/.+/)]],
      platform: ['', Validators.required]
    });

    this.scheduleForm = this.fb.group({
      daysBeforeRelease: [7, [Validators.required, Validators.min(0), Validators.max(365)]],
      releaseTimeHour: [8, [Validators.required, Validators.min(0), Validators.max(23)]],
      releaseTimeMinute: [0, [Validators.required, Validators.min(0), Validators.max(59)]]
    });

    this.credentialsForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  loadCourse(id: string): void {
    this.courseService.getCourse(id).subscribe({
      next: (course: Course) => {
        this.basicForm.patchValue({
          name: course.name,
          bookingUrl: course.bookingUrl,
          platform: course.platform
        });

        this.scheduleForm.patchValue({
          daysBeforeRelease: course.releaseSchedule.daysBeforeRelease,
          releaseTimeHour: course.releaseSchedule.releaseTimeHour,
          releaseTimeMinute: course.releaseSchedule.releaseTimeMinute
        });

        // Don't prefill password for security
      }
    });
  }

  submitForm(): void {
    if (!this.basicForm.valid || !this.scheduleForm.valid || !this.credentialsForm.valid) {
      return;
    }

    this.submitting = true;

    const request: CreateCourseRequest = {
      name: this.basicForm.get('name')?.value,
      bookingUrl: this.basicForm.get('bookingUrl')?.value,
      platform: this.basicForm.get('platform')?.value,
      releaseSchedule: {
        daysBeforeRelease: this.scheduleForm.get('daysBeforeRelease')?.value,
        releaseTimeHour: this.scheduleForm.get('releaseTimeHour')?.value,
        releaseTimeMinute: this.scheduleForm.get('releaseTimeMinute')?.value
      },
      credentials: {
        email: this.credentialsForm.get('email')?.value,
        password: this.credentialsForm.get('password')?.value
      }
    };

    const credentials = {
      email: this.credentialsForm.get('email')?.value,
      password: this.credentialsForm.get('password')?.value
    };

    const operation = this.editMode && this.courseId
      ? this.courseService.updateCourse(this.courseId, request)
      : this.courseService.createCourse(request);

    operation.subscribe({
      next: (course: Course) => {
        const courseId = course.id;
        this.courseService.saveCredentials(courseId, credentials).subscribe({
          next: () => this.router.navigate(['/dashboard']),
          error: () => {
            // Course saved — navigate even if credentials call fails
            this.router.navigate(['/dashboard']);
          }
        });
      },
      error: () => {
        this.submitting = false;
      }
    });
  }

  getPlatformLabel(platform: CoursePlatform): string {
    return getPlatformLabel(platform);
  }

  formatTime(hour: number | null, minute: number | null): string {
    const h = String(hour || 0).padStart(2, '0');
    const m = String(minute || 0).padStart(2, '0');
    return `${h}:${m}`;
  }
}
