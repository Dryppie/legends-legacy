import { inject } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  CanActivateFn,
  Router,
  RouterStateSnapshot,
} from '@angular/router';
import { filter, firstValueFrom, Observable, take } from 'rxjs';
import { AuthService } from '../../services/api/auth/auth.service';

export const authGuard: CanActivateFn = async (
  route: ActivatedRouteSnapshot,
  state: RouterStateSnapshot,
): Promise<boolean> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  authService.returnUrl = state.url;

  const isAuthed = await firstValueFrom(
    authService.isAuthenticated$.pipe(
      filter((value) => value !== null), // Wait for non-null authentication status
      take(1),
    ),
  );

  if (isAuthed) {
    return true; // Allow access to the authenticated route
  }

  // Redirect unauthenticated users to the login or appropriate route
  await router.navigateByUrl('/login');
  return false;
};
