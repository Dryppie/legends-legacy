import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../../services/auth/auth.service';
import { filter, firstValueFrom, take } from 'rxjs';

export const publicGuard: CanActivateFn = async (): Promise<boolean> => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthed = await firstValueFrom(
    authService.isAuthenticated$.pipe(
      filter((value) => value !== null),
      take(1),
    ),
  );

  if (!isAuthed) {
    return true;
  }

  // Redirect authenticated users to the `/game` route
  await router.navigateByUrl('/game');
  return false;
};
