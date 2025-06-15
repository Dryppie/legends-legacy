import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../services/api/auth/auth.service';

export const publicGuard: CanActivateFn = async (): Promise<boolean> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthed = authService.isAuthenticated();

  if (!isAuthed) {
    return true;
  }

  // Redirect authenticated users to the `/game` route
  await router.navigateByUrl('/game');
  return false;
};
