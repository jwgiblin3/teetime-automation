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
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
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
