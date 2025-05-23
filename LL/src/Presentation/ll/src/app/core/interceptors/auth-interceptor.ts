import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import {
  catchError,
  Observable,
  Subject,
  switchMap,
  take,
  tap,
  throwError,
} from 'rxjs';
import { AuthService } from '../services/api/auth/auth.service';
import { Injectable } from '@angular/core';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshInProgress = false;
  private refreshFinished$ = new Subject<boolean>();

  constructor(private auth: AuthService) {}

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler,
  ): Observable<HttpEvent<any>> {
    return next.handle(req).pipe(
      catchError((err: HttpErrorResponse) => {
        /* not an auth problem – just bubble it up */
        if (
          err.status !== 401 ||
          req.url.endsWith('createNewTokens') ||
          req.url.endsWith('/character')
        ) {
          return throwError(() => err);
        }

        /* a refresh is already running → wait for it */
        if (this.refreshInProgress) {
          return this.refreshFinished$.pipe(
            take(1),
            switchMap((ok) =>
              ok
                ? next.handle(req) // retry original call
                : throwError(() => err),
            ),
          ); // still 401 → logout
        }

        /* first request that noticed the 401 → start refresh */
        this.refreshInProgress = true;
        return this.auth.tryRefresh().pipe(
          tap((ok) => {
            this.refreshInProgress = false;
            this.refreshFinished$.next(ok); // wake up the queue
          }),
          switchMap((ok) =>
            ok
              ? next.handle(req) // retry original call
              : throwError(() => err),
          ), // refresh failed
        );
      }),
    );
  }
}
