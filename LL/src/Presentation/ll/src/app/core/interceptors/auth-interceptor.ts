import {
  HttpErrorResponse,
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
} from '@angular/common/http';
import { catchError, Observable, Subject, switchMap, throwError } from 'rxjs';
import { AuthService } from '../services/api/auth/auth.service';
import { effect, Injectable } from '@angular/core';
import { EventBusService } from '../services/client-side/event-bus/event-bus.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  /* 401-recovery (unchanged) */
  private refreshing401 = false;
  private queue: {
    req: HttpRequest<any>;
    next: HttpHandler;
    subject: Subject<HttpEvent<any>>;
  }[] = [];

  constructor(
    private auth: AuthService,
    private eventBus: EventBusService,
  ) {
    /* flush queue on manual logout */
    effect(() => {
      if (this.eventBus.logout()) {
        this.flushQueue(new HttpErrorResponse({ status: 401 }));
      }
    });
  }

  intercept(
    req: HttpRequest<any>,
    next: HttpHandler,
  ): Observable<HttpEvent<any>> {
    /* Anonymous auth routes must skip the pre-check or refresh would loop. */
    if (this.isAnonymousAuthRequest(req)) {
      return next.handle(req);
    }

    if (!this.auth.isAuthenticated()) {
      return next.handle(this.withAuthHeader(req));
    }

    /* 1️⃣  guarantee a fresh token before the call leaves the browser */
    return this.auth.ensureValidToken().pipe(
      switchMap(() => next.handle(this.withAuthHeader(req))),
      catchError((err) => this.handle401(err, req, next)),
    );
  }

  /* ------------------------------------------------------------------ */
  /* existing single-flight 401 handler kept for extreme edge cases      */
  /* ------------------------------------------------------------------ */
  private handle401(
    err: HttpErrorResponse,
    req: HttpRequest<any>,
    next: HttpHandler,
  ): Observable<HttpEvent<any>> {
    if (err.status !== 401) {
      return throwError(() => err);
    }

    /* 🔹 NEW — if nobody is logged in, just propagate the 401
     so the router-guard / global error-handler can redirect
     to the login page instead of leaving the view blank. */
    if (!this.auth.isAuthenticated()) {
      return throwError(() => err);
    }

    /* ---------- existing retry-queue logic below ---------- */

    if (!this.refreshing401) {
      this.refreshing401 = true;

      this.auth.refreshSession().subscribe({
        next: () => {
          this.refreshing401 = false;
          this.flushQueue(); // retry queued requests
        },
        error: () => {
          this.refreshing401 = false;
          this.flushQueue(err); // fail queued requests
          this.auth.logout();
        },
      });
    }

    const retry$ = new Subject<HttpEvent<any>>();
    this.queue.push({ req, next, subject: retry$ });
    return retry$.asObservable();
  }

  private flushQueue(error?: any) {
    while (this.queue.length) {
      const { req, next, subject } = this.queue.shift()!;
      if (error) {
        subject.error(error);
      } else {
        next.handle(this.withAuthHeader(req)).subscribe({
          next: (res) => subject.next(res),
          error: (err) => subject.error(err),
          complete: () => subject.complete(),
        });
      }
    }
  }

  private withAuthHeader(req: HttpRequest<any>): HttpRequest<any> {
    const token = this.auth.getAccessToken();
    if (!token) {
      return req;
    }

    return req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`,
      },
    });
  }

  private isAnonymousAuthRequest(req: HttpRequest<any>): boolean {
    const url = req.url.toLowerCase();
    return (
      url.includes('/auth/login') ||
      url.includes('/auth/loginasguest') ||
      url.includes('/auth/register') ||
      url.includes('/auth/google') ||
      url.includes('/auth/createnewtokens') ||
      url.includes('/auth/logout')
    );
  }
}
