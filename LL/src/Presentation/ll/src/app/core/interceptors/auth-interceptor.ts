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
import { effect, Injectable } from '@angular/core';
import { EventBusService } from '../services/client-side/event-bus/event-bus.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  private refreshing = false;
  private queue: Subject<HttpEvent<any>>[] = [];

  constructor(
    private auth: AuthService,
    private eventBus: EventBusService,
  ) {
    // Flush queued requests after a manual logout
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
    return next.handle(req).pipe(
      catchError((err) => {
        if (err.status !== 401) return throwError(() => err);

        // 401 ⇒ try one refresh globally
        if (!this.refreshing) {
          this.refreshing = true;
          this.auth['tryRefresh']() // private but okay inside same file, or export it.
            .subscribe({
              next: (exp) => {
                this.refreshing = false;
                if (exp) {
                  this.auth['afterSuccessfulAuth'](exp);
                  this.flushQueue(); // retry queued requests
                } else {
                  this.flushQueue(err); // fail all queued requests
                }
              },
              error: () => {
                this.refreshing = false;
                this.flushQueue(err);
                this.auth.logout();
              },
            });
        }

        // queue current request
        const retry$ = new Subject<HttpEvent<any>>();
        this.queue.push(retry$);
        return retry$.asObservable();
      }),
    );
  }

  private flushQueue(error?: any) {
    while (this.queue.length) {
      const sub = this.queue.shift()!;
      if (error) sub.error(error);
      else sub.complete();
    }
  }
}
