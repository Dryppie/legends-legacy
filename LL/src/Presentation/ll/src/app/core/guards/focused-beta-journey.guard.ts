import { inject } from '@angular/core';
import { CanActivateFn, CanMatchFn, Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { CharacterStateService } from '../services/api/character/character-state.service';
import { QuestStateService } from '../services/api/quest/quest-state.service';
import {
  isPlayerJourneyOnboardingComplete,
  PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL,
} from '../services/client-side/player-journey/player-journey';

const JOURNEY_HOME = '/game/character/character-overview';

export function isFocusedBetaRegionAllowed(
  regionId: string | null,
  characterLevel = 1,
): boolean {
  if (!regionId) return false;
  return (
    regionId.toLowerCase() === 'shenic' ||
    characterLevel >= PLAYER_JOURNEY_FULL_GAME_UNLOCK_LEVEL
  );
}

export const focusedBetaUnavailableGuard: CanActivateFn = () => {
  if (isFocusedBetaFeatureAvailable(1, true)) return true;
  return inject(Router).parseUrl(JOURNEY_HOME);
};

export const focusedBetaUnavailableMatchGuard: CanMatchFn = () => {
  if (isFocusedBetaFeatureAvailable(1, true)) return true;
  return inject(Router).parseUrl(JOURNEY_HOME);
};

export function focusedBetaMinimumLevelGuard(
  minimumLevel: number,
): CanActivateFn {
  return () => {
    if (isFocusedBetaFeatureAvailable(minimumLevel, true)) return true;
    return inject(Router).parseUrl(JOURNEY_HOME);
  };
}

export const focusedBetaRegionGuard: CanActivateFn = (route) => {
  const characterLevel = currentCharacterLevel();
  if (
    !environment.features.focusedBetaJourney ||
    isFocusedBetaRegionAllowed(route.paramMap.get('id'), characterLevel)
  ) {
    return true;
  }

  return inject(Router).parseUrl('/game/world/shenic');
};

function isFocusedBetaFeatureAvailable(
  minimumLevel: number,
  requireCompletedOnboarding: boolean,
): boolean {
  if (!environment.features.focusedBetaJourney) return true;

  const characterLevel = currentCharacterLevel();
  if (characterLevel < minimumLevel) return false;
  if (!requireCompletedOnboarding) return true;

  return isPlayerJourneyOnboardingComplete(
    inject(QuestStateService).journal(),
    characterLevel,
  );
}

function currentCharacterLevel(): number {
  return inject(CharacterStateService).currentCharacter()?.level ?? 1;
}
