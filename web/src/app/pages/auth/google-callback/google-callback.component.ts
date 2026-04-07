import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../services/auth.service';

@Component({
  selector: 'app-google-callback',
  standalone: true,
  imports: [CommonModule, MatProgressSpinnerModule],
  templateUrl: './google-callback.component.html',
  styleUrls: ['./google-callback.component.css']
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
