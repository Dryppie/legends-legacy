import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { environment } from '../../../environments/environment';

export const raidFeatureGuard: CanActivateFn = () => {
  if (environment.features.raids) {
    return true;
  }

  return inject(Router).createUrlTree(['/game/world']);
};
