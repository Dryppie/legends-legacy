import {
  APP_INITIALIZER,
  ApplicationConfig,
  provideZoneChangeDetection,
} from '@angular/core';
import { provideRouter } from '@angular/router';

import { routes } from './app.routes';
import { provideAnimations } from '@angular/platform-browser/animations';
import {
  HTTP_INTERCEPTORS,
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
import { AuthInterceptor } from './core/interceptors/auth-interceptor';
import { RealTimeFacade } from './core/services/real-time/real-time-facade';

export function initializeApp(auth: AuthService, realTime: RealTimeFacade) {
  return () => {
    firstValueFrom(auth.initAuth()).catch(() => Promise.resolve()); // Make sure this resolves only after checking/refreshing auth
    realTime.initialize(); // Optional, if you need to manually trigger logic
  };
}
export const appConfig: ApplicationConfig = {
  providers: [
    {
      provide: PLAYBACK_STRATEGIES,
      useFactory: (
        combatLogService: CombatLogService,
        levelingService: LevelingService,
      ) => ({
        [BattleType.IdleCombat]: new IdlePlaybackStrategy(
          combatLogService,
          levelingService,
        ),
        [BattleType.Colosseum]: new ColosseumPlaybackStrategy(),
      }),
      deps: [CombatLogService, LevelingService],
    },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideAnimations(),
    provideHttpClient(withInterceptorsFromDi()),
    provideAnimationsAsync(),
    AuthService,
    {
      provide: APP_INITIALIZER,
      useFactory: initializeApp,
      deps: [AuthService, RealTimeFacade],
      multi: true,
    },
    provideRouter(routes),
  ],
};
