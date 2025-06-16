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
import { TokenRefreshInterceptor } from './core/interceptors/token-refresh-interceptor';

export function initializeApp(authService: AuthService) {
  return () =>
    firstValueFrom(authService.checkAuth()).catch(() => Promise.resolve());
}
function startRealTime(realTime: RealTimeFacade) {
  return () => {}; // noop; DI only cares that the factory runs
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
    {
      provide: APP_INITIALIZER,
      useFactory: startRealTime,
      deps: [RealTimeFacade], // 👈 forces construction
      multi: true,
    },
    {
      provide: HTTP_INTERCEPTORS,
      useClass: TokenRefreshInterceptor,
      multi: true,
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
      deps: [AuthService],
      multi: true,
    },
    provideRouter(routes),
  ],
};
