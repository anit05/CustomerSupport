import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { Role } from '../models/auth.model';
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  return auth.authenticated() ? true : inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};
export const roleGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const permitted = route.data['roles'] as Role[] | undefined;
  return (!permitted || (auth.role() && permitted.includes(auth.role()!))) ? true : inject(Router).createUrlTree(['/access-denied']);
};
