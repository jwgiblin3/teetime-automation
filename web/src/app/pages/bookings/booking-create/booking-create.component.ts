import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
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
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSliderModule } from '@angular/material/slider';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { CourseService } from '../../../services/course.service';
import { BookingService } from '../../../services/booking.service';
import { Course } from '../../../models/course.models';
import { CreateBookingRequest } from '../../../models/booking.models';

@Component({
  selector: 'app-booking-create',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSliderModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatSlideToggleModule
  ],
  template: `
    <div class="create-booking-container">
      <div class="page-header">
        <h1>Book a Tee Time</h1>
      </div>

      <mat-card class="stepper-card">
        <mat-stepper #stepper>
          <!-- Step 1: Select Course -->
          <mat-step label="Select Course" [stepControl]="courseForm" editable>
            <form [formGroup]="courseForm" class="step-form">
              <mat-form-field class="full-width">
                <mat-label>Select a Course</mat-label>
                <mat-select formControlName="courseId" required>
                  <mat-option *ngFor="let course of courses" [value]="course.id">
                    {{ course.name }}
                  </mat-option>
                </mat-select>
                <mat-error *ngIf="courseForm.get('courseId')?.hasError('required')">
                  Please select a course
                </mat-error>
              </mat-form-field>

              <div class="step-actions">
                <button mat-raised-button color="primary" matStepperNext>
                  Continue
                </button>
              </div>
            </form>
          </mat-step>

          <!-- Step 2: Date and Time -->
          <mat-step label="Date & Time" [stepControl]="dateTimeForm" editable>
            <form [formGroup]="dateTimeForm" class="step-form">
              <mat-form-field class="full-width">
                <mat-label>Requested Date</mat-label>
                <input
                  matInput
                  [matDatepicker]="picker"
                  formControlName="requestedDate"
                  required
                />
                <mat-datepicker-toggle
                  matSuffix
                  [for]="picker"
                ></mat-datepicker-toggle>
                <mat-datepicker #picker></mat-datepicker>
                <mat-error *ngIf="dateTimeForm.get('requestedDate')?.hasError('required')">
                  Date is required
                </mat-error>
              </mat-form-field>

              <mat-form-field class="full-width">
                <mat-label>Preferred Time</mat-label>
                <input
                  matInput
                  type="time"
                  formControlName="preferredTime"
                  required
                />
                <mat-error *ngIf="dateTimeForm.get('preferredTime')?.hasError('required')">
                  Time is required
                </mat-error>
              </mat-form-field>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button mat-raised-button color="primary" matStepperNext>
                  Continue
                </button>
              </div>
            </form>
          </mat-step>

          <!-- Step 3: Time Window -->
          <mat-step label="Time Window" [stepControl]="timeWindowForm" editable>
            <form [formGroup]="timeWindowForm" class="step-form">
              <div class="time-window-section">
                <label class="time-window-label">Booking Time Window</label>
                <p class="time-window-description">
                  How flexible are you with the booking time? The system will attempt
                  to book within this window of your preferred time.
                </p>

                <div class="slider-container">
                  <div class="slider-label">±{{ timeWindowForm.get('timeWindowMinutes')?.value || 30 }} minutes</div>
                  <mat-slider
                    formControlName="timeWindowMinutes"
                    min="0"
                    max="120"
                    step="5"
                    class="time-slider"
                  >
                    <input matSliderThumb />
                  </mat-slider>
                  <div class="slider-ticks">
                    <span>0 min</span>
                    <span>30 min</span>
                    <span>60 min</span>
                    <span>90 min</span>
                    <span>120 min</span>
                  </div>
                </div>

                <p class="time-window-example">
                  Example: If you prefer 10:00 AM with ±30 minutes, the system will
                  attempt to book anytime between 9:30 AM and 10:30 AM.
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

          <!-- Step 4: Number of Players -->
          <mat-step label="Players" [stepControl]="playersForm" editable>
            <form [formGroup]="playersForm" class="step-form">
              <div class="players-section">
                <label class="players-label">Number of Players</label>
                <p class="players-description">How many players will be golfing?</p>

                <div class="player-buttons">
                  <button
                    *ngFor="let num of [1, 2, 3, 4]"
                    type="button"
                    class="player-button"
                    [class.selected]="playersForm.get('numberOfPlayers')?.value === num"
                    (click)="playersForm.get('numberOfPlayers')?.setValue(num)"
                  >
                    {{ num }}
                  </button>
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

          <!-- Step 5: Review -->
          <mat-step label="Review">
            <div class="review-section">
              <h3>Review Your Booking</h3>

              <div class="review-item">
                <span class="review-label">Course:</span>
                <span class="review-value">
                  {{ selectedCourseName }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Date:</span>
                <span class="review-value">
                  {{ dateTimeForm.get('requestedDate')?.value | date: 'MMMM dd, yyyy' }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Time:</span>
                <span class="review-value">
                  {{ dateTimeForm.get('preferredTime')?.value }}
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Time Window:</span>
                <span class="review-value">
                  ±{{ timeWindowForm.get('timeWindowMinutes')?.value }} minutes
                </span>
              </div>

              <div class="review-item">
                <span class="review-label">Players:</span>
                <span class="review-value">
                  {{ playersForm.get('numberOfPlayers')?.value }}
                </span>
              </div>

              <div class="step-actions">
                <button mat-button matStepperPrevious>Back</button>
                <button
                  mat-raised-button
                  color="primary"
                  (click)="submitBooking()"
                  [disabled]="submitting"
                >
                  <mat-spinner
                    *ngIf="submitting"
                    diameter="20"
                    class="inline-spinner"
                  ></mat-spinner>
                  {{ submitting ? 'Creating Booking...' : 'Create Booking' }}
                </button>
              </div>
            </div>
          </mat-step>
        </mat-stepper>
      </mat-card>
    </div>
  `,
  styles: [`
    .create-booking-container {
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

    .step-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 1rem;
      padding-top: 1rem;
      border-top: 1px solid #303030;
    }

    .time-window-section {
      padding: 1.5rem 0;
    }

    .time-window-label {
      display: block;
      color: #c8e6c9;
      font-weight: 600;
      margin-bottom: 0.5rem;
      text-transform: uppercase;
      font-size: 0.85rem;
      letter-spacing: 0.5px;
    }

    .time-window-description {
      color: #999999;
      margin-bottom: 1.5rem;
      font-size: 0.95rem;
    }

    .slider-container {
      margin: 2rem 0;
    }

    .slider-label {
      color: #c8e6c9;
      font-size: 1.25rem;
      font-weight: 600;
      margin-bottom: 1rem;
      text-align: center;
    }

    .time-slider {
      width: 100%;
      margin: 1rem 0;
    }

    .slider-ticks {
      display: flex;
      justify-content: space-between;
      color: #666666;
      font-size: 0.75rem;
      margin-top: 0.5rem;
    }

    .time-window-example {
      color: #999999;
      font-size: 0.9rem;
      font-style: italic;
      margin-top: 1rem;
    }

    .players-section {
      padding: 1.5rem 0;
    }

    .players-label {
      display: block;
      color: #c8e6c9;
      font-weight: 600;
      margin-bottom: 0.5rem;
      text-transform: uppercase;
      font-size: 0.85rem;
      letter-spacing: 0.5px;
    }

    .players-description {
      color: #999999;
      margin-bottom: 1.5rem;
      font-size: 0.95rem;
    }

    .player-buttons {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 1rem;
      margin-bottom: 1rem;
    }

    .player-button {
      padding: 1rem;
      border: 2px solid #303030;
      background-color: transparent;
      color: #999999;
      border-radius: 8px;
      cursor: pointer;
      font-size: 1.1rem;
      font-weight: 600;
      transition: all 0.2s;

      &:hover {
        border-color: #43a047;
        color: #43a047;
      }

      &.selected {
        background-color: #2e7d32;
        border-color: #2e7d32;
        color: #ffffff;
      }
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
      font-size: 1.05rem;
      font-weight: 500;
    }

    .inline-spinner {
      display: inline-block !important;
      margin-right: 0.5rem;
    }

    @media (max-width: 768px) {
      .create-booking-container {
        padding: 1rem;
      }

      .step-form {
        padding: 1.5rem;
      }

      .player-buttons {
        grid-template-columns: repeat(2, 1fr);
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
export class BookingCreateComponent implements OnInit {
  courseForm!: FormGroup;
  dateTimeForm!: FormGroup;
  timeWindowForm!: FormGroup;
  playersForm!: FormGroup;

  courses: Course[] = [];
  selectedCourseName = '';
  submitting = false;

  constructor(
    private fb: FormBuilder,
    private courseService: CourseService,
    private bookingService: BookingService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.initializeForms();
    this.loadCourses();
  }

  initializeForms(): void {
    this.courseForm = this.fb.group({
      courseId: ['', Validators.required]
    });

    this.dateTimeForm = this.fb.group({
      requestedDate: ['', Validators.required],
      preferredTime: ['', Validators.required]
    });

    this.timeWindowForm = this.fb.group({
      timeWindowMinutes: [30, Validators.required]
    });

    this.playersForm = this.fb.group({
      numberOfPlayers: [2, Validators.required]
    });

    this.courseForm.get('courseId')?.valueChanges.subscribe((courseId) => {
      const course = this.courses.find((c) => c.id === courseId);
      this.selectedCourseName = course?.name || '';
    });
  }

  loadCourses(): void {
    this.courseService.getCourses().subscribe({
      next: (courses) => {
        this.courses = courses;
      }
    });
  }

  submitBooking(): void {
    if (
      !this.courseForm.valid ||
      !this.dateTimeForm.valid ||
      !this.timeWindowForm.valid ||
      !this.playersForm.valid
    ) {
      return;
    }

    this.submitting = true;

    const request: CreateBookingRequest = {
      courseId: this.courseForm.get('courseId')?.value,
      requestedDate: this.dateTimeForm.get('requestedDate')?.value,
      preferredTime: this.dateTimeForm.get('preferredTime')?.value,
      timeWindowMinutes: this.timeWindowForm.get('timeWindowMinutes')?.value,
      numberOfPlayers: this.playersForm.get('numberOfPlayers')?.value
    };

    this.bookingService.createBooking(request).subscribe({
      next: (booking) => {
        this.router.navigate(['/bookings', booking.id]);
      },
      error: () => {
        this.submitting = false;
      }
    });
  }
}
