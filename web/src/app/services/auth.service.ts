import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  User,
  UserRole,
  DecodedToken
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private apiUrl = `${environment.apiUrl}/auth`;
  private tokenKey = 'teetime_token';
  private currentUserSubject = new BehaviorSubject<User | null>(this.getCurrentUserFromStorage());

  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.loadUserFromToken();
  }

  login(email: string, password: string): Observable<AuthResponse> {
    const request: LoginRequest = { email, password };
    return this.http.post<AuthResponse>(`${this.apiUrl}/login`, request).pipe(
      tap((response) => this.handleAuthResponse(response))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/register`, request).pipe(
      tap((response) => this.handleAuthResponse(response))
    );
  }

  loginWithGoogle(): void {
    const clientId = environment.googleClientId;
    const redirectUri = `${window.location.origin}/auth/google-callback`;
    const scope = 'openid email profile';
    const responseType = 'code';

    const googleAuthUrl = `https://accounts.google.com/o/oauth2/v2/auth?client_id=${clientId}&redirect_uri=${encodeURIComponent(redirectUri)}&response_type=${responseType}&scope=${encodeURIComponent(scope)}`;
    window.location.href = googleAuthUrl;
  }

  handleGoogleCallback(code: string): Observable<AuthResponse> {
    const redirectUri = `${window.location.origin}/auth/google-callback`;
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/google/callback`, { code, redirectUri })
      .pipe(tap((response) => this.handleAuthResponse(response)));
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    const token = this.getToken();
    if (!token) {
      return false;
    }

    try {
      const decoded = this.decodeToken(token);
      const now = Date.now().valueOf() / 1000;
      return decoded.exp > now;
    } catch {
      return false;
    }
  }

  isAdmin(): boolean {
    const user = this.getCurrentUser();
    return user ? user.role === UserRole.ADMIN : false;
  }

  getCurrentUser(): User | null {
    return this.currentUserSubject.value;
  }

  private handleAuthResponse(response: AuthResponse): void {
    localStorage.setItem(this.tokenKey, response.accessToken);
    this.currentUserSubject.next({
      ...response.user,
      role: response.user.isAdmin ? UserRole.ADMIN : UserRole.USER
    });
  }

  private loadUserFromToken(): void {
    const token = this.getToken();
    if (token && this.isLoggedIn()) {
      try {
        const decoded = this.decodeToken(token);
        const user: User = {
          userId: parseInt(decoded.sub, 10),
          email: decoded.email,
          firstName: decoded.firstName,
          lastName: decoded.lastName,
          role: decoded.role,
          isAdmin: decoded.role === UserRole.ADMIN,
          isActive: true,
          createdAt: new Date(),
          updatedAt: new Date()
        };
        this.currentUserSubject.next(user);
      } catch {
        this.logout();
      }
    }
  }

  private getCurrentUserFromStorage(): User | null {
    const token = this.getToken();
    if (token) {
      try {
        const decoded = this.decodeToken(token);
        const now = Date.now().valueOf() / 1000;
        if (decoded.exp > now) {
          return {
            userId: parseInt(decoded.sub, 10),
            email: decoded.email,
            firstName: decoded.firstName,
            lastName: decoded.lastName,
            role: decoded.role,
            isAdmin: decoded.role === UserRole.ADMIN,
            isActive: true,
            createdAt: new Date(),
            updatedAt: new Date()
          };
        }
      } catch {
        return null;
      }
    }
    return null;
  }

  private decodeToken(token: string): DecodedToken {
    const parts = token.split('.');
    if (parts.length !== 3) {
      throw new Error('Invalid token');
    }

    const decoded = JSON.parse(
      atob(parts[1].replace(/-/g, '+').replace(/_/g, '/'))
    );
    return decoded as DecodedToken;
  }
}
