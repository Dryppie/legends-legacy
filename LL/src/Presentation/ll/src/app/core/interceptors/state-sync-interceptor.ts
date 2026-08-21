import { Injectable, Injector } from '@angular/core';
import {
  HttpEvent,
  HttpHandler,
  HttpInterceptor,
  HttpRequest,
  HttpResponse,
} from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { StateSyncCoordinator } from '../services/real-time/game-realtime/state-sync-coordinator.service';
import {
  FORCE_STATE_SYNC_RESPONSE_REFRESH,
  STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE,
} from './state-sync-context';

const STATE_REVISIONS_HEADER = 'X-LL-State-Revisions';
const mutationMethods = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

@Injectable()
export class StateSyncInterceptor implements HttpInterceptor {
  constructor(private readonly injector: Injector) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      tap((event) => {
        if (
          !(event instanceof HttpResponse) ||
          !mutationMethods.has(request.method)
        ) {
          return;
        }

        const encodedRevisions = event.headers.get(STATE_REVISIONS_HEADER);
        if (!encodedRevisions) return;

        let revisions: Record<string, number>;
        try {
          revisions = JSON.parse(encodedRevisions) as Record<string, number>;
        } catch {
          console.warn('Ignoring malformed state revision response header');
          return;
        }

        // Run after the response subscriber has applied its snapshot. If an older
        // mutation response arrived late, the forced refresh repairs that overwrite.
        queueMicrotask(() =>
          this.injector
            .get(StateSyncCoordinator)
            .acceptMutationResponse(
              revisions,
              request.context.get(FORCE_STATE_SYNC_RESPONSE_REFRESH),
              request.context.get(STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE),
            ),
        );
      }),
    );
  }
}
