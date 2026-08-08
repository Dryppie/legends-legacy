import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export const maintenanceGuard: CanActivateFn = () => {
  if (!environment.maintenance.enabled) {
    return true;
  }

  return inject(Router).createUrlTree(['/login']);
};
