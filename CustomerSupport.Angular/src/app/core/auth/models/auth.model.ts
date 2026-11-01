export type Role = 'Customer' | 'SupportExecutive' | 'Administrator';
export interface User {
  id: number;
  fullName: string;
  email: string;
  phoneNumber?: string | null;
  role: Role;
  isActive: boolean;
}
export interface CurrentUser {
  userId: number;
  fullName: string;
  email: string;
  role: Role;
}
export interface AuthResponse {
  user: User;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
}
export interface LoginRequest {
  email: string;
  password: string;
}
export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string | null;
  password: string;
  confirmPassword: string;
}
export interface RefreshTokenRequest {
  refreshToken: string;
}
export interface Session extends AuthResponse {
}
