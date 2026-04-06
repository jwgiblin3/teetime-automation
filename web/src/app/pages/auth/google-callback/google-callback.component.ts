import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  template: `
    <div class="callback-container">
      <mat-spinner diameter="48"></mat-spinner>
      <p class="message">{{ message }}</p>
    </div>
  `,
  styles: [`
    .callback-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      min-height: calc(100vh - 64px);
      gap: 1.5rem;
      background: linear-gradient(135deg, #0d3817 0%, #1b5e20 100%);
    }

    .message {
      color: #c8e6c9;
      font-size: 1rem;
      margin: 0;
    }
  `]
})
export class GoogleCallbackComponent implements OnInit {
  message = 'Completing sign-in…';

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code');
    const error = this.route.snapshot.queryParamMap.get('error');

    if (error || !code) {
      this.message = 'Sign-in was cancelled or failed. Redirecting…';
      setTimeout(() => this.router.navigate(['/login']), 1500);
      return;
    }

    this.authService.handleGoogleCallback(code).subscribe({
      next: () => this.router.navigate(['/dashboard']),
      error: () => {
        this.message = 'Sign-in failed. Redirecting…';
        setTimeout(() => this.router.navigate(['/login']), 1500);
      }
    });
  }
}
