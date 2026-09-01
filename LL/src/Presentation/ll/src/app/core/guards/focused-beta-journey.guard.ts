import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { environment } from '../../../environments/environment';

const JOURNEY_HOME = '/game/character/character-overview';

export function isFocusedBetaRegionAllowed(regionId: string | null): boolean {
  return regionId?.toLowerCase() === 'shenic';
}

export const focusedBetaUnavailableGuard: CanActivateFn = () => {
  if (!environment.features.focusedBetaJourney) return true;
  return inject(Router).parseUrl(JOURNEY_HOME);
};

export const focusedBetaUnavailableMatchGuard: CanMatchFn = () => {
  if (!environment.features.focusedBetaJourney) return true;
  return inject(Router).parseUrl(JOURNEY_HOME);
};

export const focusedBetaRegionGuard: CanActivateFn = (route) => {
  if (
    !environment.features.focusedBetaJourney ||
    isFocusedBetaRegionAllowed(route.paramMap.get('id'))
  ) {
    return true;
  }

  return inject(Router).parseUrl('/game/world/shenic');
};
