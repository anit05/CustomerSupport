import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, shareReplay, switchMap, throwError, Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { API_BASE_URL } from '../../config/api.config';
let inFlightRefresh: Observable<boolean> | null = null;
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  // Do not send credentials to other origins, and do not intercept unrelated HTTP calls.
  if (!request.url.startsWith(`${API_BASE_URL}/`))
    return next(request);
  const token = auth.accessToken();
  const authCall = /\/api\/auth\/(login|register|refresh-token|logout)(\?|$)/.test(request.url);
  const send = token && !authCall ? request.clone({ setHeaders: { Authorization: `Bearer ${token}`, Accept: 'application/json' } })
    : request.clone({ setHeaders: { Accept: 'application/json' } });
  return next(send).pipe(catchError((error: HttpErrorResponse) => {
    if (error.status === 401 && !authCall && auth.refreshToken()) {
      if (!inFlightRefresh)
        inFlightRefresh = auth.refresh().pipe(shareReplay(1));
      return inFlightRefresh.pipe(switchMap(ok => {
        inFlightRefresh = null;
        if (!ok) {
          void router.navigate(['/login']);
          return throwError(() => error);
        }
        return next(request.clone({ setHeaders: { Authorization: `Bearer ${auth.accessToken()}`, Accept: 'application/json' } }));
      }), catchError(retryError => { inFlightRefresh = null; return throwError(() => retryError); }));
    }
    if (error.status === 403)
      void router.navigate(['/access-denied']);
    if (error.status >= 500)
      void router.navigate(['/server-error']);
    if (error.status === 401 && !authCall) {
      auth.clear();
      void router.navigate(['/login']);
    }
    return throwError(() => error);
  }));
};
