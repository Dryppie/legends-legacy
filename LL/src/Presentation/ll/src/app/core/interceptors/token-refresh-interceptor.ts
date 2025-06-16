import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import {
  catchError,
  finalize,
  Observable,
  shareReplay,
  switchMap,
  tap,
  throwError,
} from 'rxjs';
import { AuthService } from '../services/api/auth/auth.service';
import { Injectable } from '@angular/core';
import { ApiService } from '../services/api/api.service';

@Injectable()
export class TokenRefreshInterceptor implements HttpInterceptor {
  private refreshInFlight$?: Observable<boolean>;

  constructor(
    private auth: AuthService,
    private api: ApiService,
  ) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler,
  ): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((err, src) => {
        if (err.status !== 401) return throwError(() => err);

        /** start or join a single refresh request */
        if (!this.refreshInFlight$) {
          this.refreshInFlight$ = this.auth
            .tryRefresh() // ← your existing endpoint
            .pipe(
              tap((ok) => {
                if (!ok) this.auth.logout();
              }),
              finalize(() => (this.refreshInFlight$ = undefined)),
              shareReplay({ bufferSize: 1, refCount: false }),
            );
        }

        return this.refreshInFlight$.pipe(
          switchMap(() => next.handle(req.clone())),
        );
      }),
    );
  }
}
