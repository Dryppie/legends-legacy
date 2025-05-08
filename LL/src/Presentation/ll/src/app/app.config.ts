import {
  APP_INITIALIZER,
  ApplicationConfig,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
  provideHttpClient,
  withInterceptorsFromDi,
} from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { AuthService } from './core/services/api/auth/auth.service';
import { firstValueFrom } from 'rxjs';
import { IdlePlaybackStrategy } from './core/services/client-side/combat/combat-playback/idle-playback-strategy';
import { PLAYBACK_STRATEGIES } from './core/services/client-side/combat/combat-playback/combat-playback-strategy';
import { BattleType } from './core/state/combat-state/combatState';
import { CombatLogService } from './core/services/client-side/combat/combat-log/combat-log.service';
import { LevelingService } from './core/services/client-side/leveling/leveling.service';
import { ColosseumPlaybackStrategy } from './core/services/client-side/combat/combat-playback/colosseum-playback-strategy';

export function initializeApp(authService: AuthService) {
  return () =>
    firstValueFrom(authService.checkAuth()).catch(() => Promise.resolve());
}

export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: PLAYBACK_STRATEGIES,
      useFactory: (
        combatLogService: CombatLogService,
        levelingService: LevelingService,
      ) => ({
        [BattleType.Idle]: new IdlePlaybackStrategy(
          combatLogService,
          levelingService,
        ),
        [BattleType.Colosseum]: new ColosseumPlaybackStrategy(),
      }),
      deps: [CombatLogService, LevelingService],
    },

    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    AuthService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [AuthService],
      multi: true,
    },
    provideRouter(routes),
  ],
};
