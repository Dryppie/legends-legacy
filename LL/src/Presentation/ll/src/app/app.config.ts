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
import { AuthInterceptor } from './core/interceptors/auth-interceptor';
import { RealTimeFacade } from './core/services/real-time/real-time-facade';
import { TimeSyncService } from './core/services/api/time-sync/time-sync.service';

export function initializeApp(authService: AuthService) {
  return () =>
    firstValueFrom(authService.checkAuth()).catch(() => Promise.resolve());
}
function startRealTime(realTime: RealTimeFacade) {
  return () => realTime.initialize();
}
export function initializeTimeSync(timeSyncService: TimeSyncService) {
  return () => timeSyncService.sync();
}

export const appConfig: ApplicationConfig = {
  providers: [
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
    {
      provide: APP_INITIALIZER,
      useFactory: startRealTime,
      deps: [RealTimeFacade],
      multi: true,
    },
    {
      provide: APP_INITIALIZER,
      useFactory: initializeTimeSync,
      deps: [TimeSyncService],
      multi: true,
    },
    provideRouter(routes),
  ],
};
