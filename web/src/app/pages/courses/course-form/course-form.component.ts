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
  templateUrl: './course-form.component.html',
  styleUrls: ['./course-form.component.css']
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
