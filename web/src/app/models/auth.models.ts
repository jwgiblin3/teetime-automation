export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  password: string;
  confirmPassword: string;
}

export interface AuthResponse {
  accessToken: string;
  tokenType: string;
  expiresIn: number;
  user: User;
}

export interface User {
  userId: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  isAdmin: boolean;
  isActive: boolean;
  role: UserRole;
  createdAt: Date;
  updatedAt: Date;
}

export enum UserRole {
  USER = 'user',
  ADMIN = 'admin'
}

export interface DecodedToken {
  sub: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  iat: number;
  exp: number;
}
