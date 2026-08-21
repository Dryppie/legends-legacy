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
  readDomainVersions,
  FORCE_STATE_SYNC_RESPONSE_REFRESH,
  STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE,
} from './state-sync-context';
import { StateSyncDiagnostics } from '../services/real-time/game-realtime/state-sync-diagnostics.service';
import { DomainVersionTracker } from '../services/real-time/game-realtime/domain-version-tracker.service';

const mutationMethods = new Set(['POST', 'PUT', 'PATCH', 'DELETE']);

@Injectable()
export class StateSyncInterceptor implements HttpInterceptor {
  constructor(private readonly injector: Injector) {}

  intercept(
    request: HttpRequest<unknown>,
    next: HttpHandler,
  ): Observable<HttpEvent<unknown>> {
    if (request.method === 'GET') {
      this.injector.get(StateSyncDiagnostics).recordGet(request.urlWithParams);
    }

    return next.handle(request).pipe(
      tap((event) => {
        if (
          !(event instanceof HttpResponse) ||
          !mutationMethods.has(request.method)
        ) {
          return;
        }

        const revisions = readDomainVersions(event.headers);
        if (!Object.keys(revisions).length) return;

        this.injector.get(DomainVersionTracker).observe(revisions);

        const handledScopes = request.context.get(
          STATE_SYNC_SCOPES_HANDLED_BY_RESPONSE,
        );
        this.injector
          .get(StateSyncDiagnostics)
          .recordMutation(
            request.method,
            request.urlWithParams,
            revisions,
            handledScopes,
          );

        // Run after the response subscriber has applied its snapshot. If an older
        // mutation response arrived late, the forced refresh repairs that overwrite.
        queueMicrotask(() =>
          this.injector
            .get(StateSyncCoordinator)
            .acceptMutationResponse(
              revisions,
              request.context.get(FORCE_STATE_SYNC_RESPONSE_REFRESH),
              handledScopes,
            ),
        );
      }),
    );
  }
}
