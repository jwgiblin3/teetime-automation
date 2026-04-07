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
  templateUrl: './booking-create.component.html',
  styleUrls: ['./booking-create.component.css']
})
export class BookingCreateComponent implements OnInit {
  courseForm!: FormGroup;
  dateTimeForm!: FormGroup;
  timeWindowForm!: FormGroup;
  playersForm!: FormGroup;

  courses: Course[] = [];
  selectedCourseName = '';
  submitting = false;
  minDate = new Date();



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
      requestedDate: [null, Validators.required],
      requestedTime: ['', Validators.required]
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

  formatTime(): string {
    const t: string = this.dateTimeForm?.get('requestedTime')?.value;
    if (!t) return '';
    const [hStr, mStr] = t.split(':');
    const h = parseInt(hStr, 10);
    const m = parseInt(mStr, 10);
    const h12 = h % 12 === 0 ? 12 : h % 12;
    const ampm = h < 12 ? 'AM' : 'PM';
    return `${h12}:${m.toString().padStart(2, '0')} ${ampm}`;
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

    const dateVal: Date = this.dateTimeForm.get('requestedDate')?.value;
    const requestedDate = new Date(dateVal.getFullYear(), dateVal.getMonth(), dateVal.getDate());
    const preferredTime: string = this.dateTimeForm.get('requestedTime')?.value;

    const request: CreateBookingRequest = {
      courseId: this.courseForm.get('courseId')?.value,
      requestedDate,
      preferredTime,
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
