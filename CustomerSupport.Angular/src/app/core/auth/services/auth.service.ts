import { Injectable, inject, signal, computed } from '@angular/core';
import { map, Observable, tap, catchError, of } from 'rxjs';
import { ApiClientService } from '../../http/services/api-client.service';
import { AuthResponse, CurrentUser, Role, User, RegisterRequest } from '../models/auth.model';
const KEY = 'customer-support.auth.session';
interface Session {
  user: User;
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}
function readSession(): Session | null {
  try {
    return JSON.parse((typeof sessionStorage === 'undefined' ? null : sessionStorage.getItem(KEY)) || 'null') as Session | null;
  }
  catch {
    return null;
  }
}
@Injectable({ providedIn: 'root' })
export class AuthService {
  private api = inject(ApiClientService);
  private state = signal<Session | null>(readSession());
  user = computed(() => this.state()?.user ?? null);
  role = computed<Role | null>(() => this.user()?.role ?? null);
  authenticated = computed(() => !!this.state()?.accessToken);
  accessToken(): string | null { return this.state()?.accessToken ?? null; }
  refreshToken(): string | null { return this.state()?.refreshToken ?? null; }
  login(email: string, password: string): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('api/auth/login', { email, password }).pipe(tap(response => this.save(response)));
  }
  register(body: RegisterRequest): Observable<AuthResponse> {
    return this.api.post<AuthResponse>('api/auth/register', body).pipe(tap(response => this.save(response)));
  }
  refresh(): Observable<boolean> {
    const token = this.refreshToken();
    if (!token)
      return of(false);
    return this.api.post<AuthResponse>('api/auth/refresh-token', { refreshToken: token }).pipe(tap(result => this.save(result)), map(() => true), catchError(() => { this.clear(); return of(false); }));
  }
  reloadProfile(): Observable<CurrentUser> {
    return this.api.get<CurrentUser>('api/account/me');
  }
  logout(): Observable<unknown> {
    const token = this.refreshToken();
    if (!token) {
      this.clear();
      return of(null);
    }
    return this.api.post<unknown>('api/auth/logout', { refreshToken: token }).pipe(tap(() => this.clear()), catchError(() => { this.clear(); return of(null); }));
  }
  clear(): void {
    this.state.set(null);
    if (typeof sessionStorage !== 'undefined')
      sessionStorage.removeItem(KEY);
  }
  private save(data: AuthResponse): void {
    const session: Session = {
      user: data.user, accessToken: data.accessToken, refreshToken: data.refreshToken,
      accessTokenExpiresAt: data.accessTokenExpiresAt, refreshTokenExpiresAt: data.refreshTokenExpiresAt
    };
    this.state.set(session);
    if (typeof sessionStorage !== 'undefined')
      sessionStorage.setItem(KEY, JSON.stringify(session));
  }
  home(): string {
    switch (this.role()) {
      case 'Administrator': return '/admin/dashboard';
      case 'SupportExecutive': return '/support/dashboard';
      default: return '/customer/dashboard';
    }
  }
}
