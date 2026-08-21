import { Injectable, Injector, signal, untracked } from '@angular/core';
import {
  firstValueFrom,
  isObservable,
  Observable,
  type ObservableInput,
} from 'rxjs';
import { StateSyncService } from '../../api/state-sync/state-sync.service';
import {
  StateInvalidated,
  StateSyncCheckpoint,
  StateSyncScope,
} from './game-realtime-contracts';
import { StateSyncDiagnostics } from './state-sync-diagnostics.service';
import { DomainVersionTracker } from './domain-version-tracker.service';
import { RealtimeUpdateDeduper } from './realtime-deduplication';

interface StateSyncRegistration {
  key: string;
  refresh: StateSyncRefresh;
  shouldRefresh: () => boolean;
  lastRefreshRevision: number;
  inFlight: boolean;
  retryAttempt: number;
  retryTimeoutId?: number;
  lastError?: unknown;
  generation: number;
}

export type StateSyncRefreshResult =
  | PromiseLike<unknown>
  | ObservableInput<unknown>;
export type StateSyncRefresh = () => StateSyncRefreshResult;

export interface StateSyncRegistrationStatus {
  scope: StateSyncScope;
  key: string;
  targetRevision: number;
  appliedRevision: number;
  stale: boolean;
  refreshing: boolean;
  retryAttempt: number;
  lastError: unknown | null;
}

@Injectable({ providedIn: 'root' })
export class StateSyncCoordinator {
  private readonly registrations = new Map<
    StateSyncScope,
    Map<string, StateSyncRegistration>
  >();
  private readonly revisions = new Map<StateSyncScope, number>();
  private readonly pendingRefreshes = new Map<StateSyncScope, number>();
  private readonly rejectedMutationRevisions = new Map<
    StateSyncScope,
    number
  >();
  private readonly updateDeduper = new RealtimeUpdateDeduper(1_000);
  private reconcilePromise?: Promise<void>;
  private reconcileRetryAttempt = 0;
  private reconcileRetryTimeoutId?: number;
  private initialized = false;
  private lastBlurAt?: number;
  private readonly longSuspensionMs = 5 * 60_000;
  private readonly _status = signal<StateSyncRegistrationStatus[]>([]);

  readonly status = this._status.asReadonly();

  constructor(private readonly injector: Injector) {}

  latestRevision(scope: StateSyncScope): number {
    return this.revisions.get(scope) ?? 0;
  }

  resetScope(scope: StateSyncScope): void {
    this.revisions.delete(scope);
    this.rejectedMutationRevisions.delete(scope);
    (
      this.injector.get(
        DomainVersionTracker,
        null,
      ) as Partial<DomainVersionTracker> | null
    )?.resetScope?.(scope);

    const pendingRefresh = this.pendingRefreshes.get(scope);
    if (pendingRefresh !== undefined) {
      window.clearTimeout(pendingRefresh);
      this.pendingRefreshes.delete(scope);
    }

    const registrations = this.registrations.get(scope);
    if (registrations) {
      for (const registration of registrations.values()) {
        if (registration.retryTimeoutId !== undefined) {
          window.clearTimeout(registration.retryTimeoutId);
        }
        registration.generation += 1;
        registration.lastRefreshRevision = 0;
        registration.inFlight = false;
        registration.retryAttempt = 0;
        registration.retryTimeoutId = undefined;
        registration.lastError = undefined;
      }
    }
    this.publishStatus();
  }

  initialize(): void {
    if (this.initialized) return;
    this.initialized = true;
    window.addEventListener('blur', this.handleBlur);
    window.addEventListener('focus', this.handleFocus);
    window.addEventListener('online', this.handleOnline);
  }

  dispose(): void {
    if (this.initialized) {
      window.removeEventListener('blur', this.handleBlur);
      window.removeEventListener('focus', this.handleFocus);
      window.removeEventListener('online', this.handleOnline);
    }
    this.initialized = false;
    this.lastBlurAt = undefined;
    this.revisions.clear();
    (
      this.injector.get(
        DomainVersionTracker,
        null,
      ) as Partial<DomainVersionTracker> | null
    )?.reset?.();
    this.updateDeduper.clear();
    this.rejectedMutationRevisions.clear();
    for (const timeoutId of this.pendingRefreshes.values()) {
      window.clearTimeout(timeoutId);
    }
    this.pendingRefreshes.clear();
    if (this.reconcileRetryTimeoutId !== undefined) {
      window.clearTimeout(this.reconcileRetryTimeoutId);
      this.reconcileRetryTimeoutId = undefined;
    }
    this.reconcileRetryAttempt = 0;
    for (const scopedRegistrations of this.registrations.values()) {
      for (const registration of scopedRegistrations.values()) {
        if (registration.retryTimeoutId !== undefined) {
          window.clearTimeout(registration.retryTimeoutId);
        }
        registration.lastRefreshRevision = 0;
        registration.inFlight = false;
        registration.retryAttempt = 0;
        registration.retryTimeoutId = undefined;
        registration.lastError = undefined;
      }
    }
    this.publishStatus();
  }

  register(
    scope: StateSyncScope,
    key: string,
    refresh: StateSyncRefresh,
    shouldRefresh: () => boolean = () => true,
  ): () => void {
    let scoped = this.registrations.get(scope);
    if (!scoped) {
      scoped = new Map<string, StateSyncRegistration>();
      this.registrations.set(scope, scoped);
    }

    const previous = scoped.get(key);
    if (previous?.retryTimeoutId !== undefined) {
      window.clearTimeout(previous.retryTimeoutId);
    }

    const registration: StateSyncRegistration = {
      key,
      refresh,
      shouldRefresh,
      lastRefreshRevision: this.revisions.get(scope) ?? 0,
      inFlight: false,
      retryAttempt: 0,
      generation: 0,
    };
    scoped.set(key, registration);
    this.publishStatus();

    return () => {
      if (scoped?.get(key) !== registration) return;
      if (registration.retryTimeoutId !== undefined) {
        window.clearTimeout(registration.retryTimeoutId);
      }
      scoped?.delete(key);
      this.publishStatus();
    };
  }

  acceptInvalidation(event: StateInvalidated, updateId?: string): void {
    if (!this.updateDeduper.shouldProcess(updateId)) return;
    this.acceptRevision(event.scope, event.revision);
  }

  acceptDomainVersion(
    scope: StateSyncScope,
    revision: number,
    updateId?: string,
  ): void {
    if (!this.updateDeduper.shouldProcess(updateId)) return;
    this.acceptRevision(scope, revision);
    this.publishStatus();
  }

  acceptSnapshotResponse(
    revisions: Readonly<Record<StateSyncScope, number>>,
    scopesIncludedInSnapshot: readonly StateSyncScope[],
  ): void {
    const included = new Set(scopesIncludedInSnapshot);
    for (const [scope, revision] of Object.entries(revisions)) {
      if (!included.has(scope)) continue;
      this.acceptHandledMutationRevision(scope, revision);
    }
    this.publishStatus();
  }

  rejectMutationResponse(scope: StateSyncScope, revision: number): void {
    this.rejectedMutationRevisions.set(
      scope,
      Math.max(this.rejectedMutationRevisions.get(scope) ?? 0, revision),
    );
    this.acceptRevision(scope, revision);
  }

  reconcile(): Promise<void> {
    if (this.reconcilePromise) return this.reconcilePromise;
    if (this.reconcileRetryTimeoutId !== undefined) {
      window.clearTimeout(this.reconcileRetryTimeoutId);
      this.reconcileRetryTimeoutId = undefined;
    }

    const service = this.injector.get(StateSyncService);
    this.reconcilePromise = firstValueFrom(service.getCheckpoint())
      .then((checkpoint) => {
        this.reconcileRetryAttempt = 0;
        this.acceptCheckpoint(checkpoint);
      })
      .catch((error) => {
        console.warn('State checkpoint reconciliation failed', error);
        this.scheduleReconcileRetry();
      })
      .finally(() => {
        this.reconcilePromise = undefined;
      });
    return this.reconcilePromise;
  }

  private scheduleReconcileRetry(): void {
    if (!this.initialized || this.reconcileRetryTimeoutId !== undefined) return;

    this.reconcileRetryAttempt += 1;
    const delay = Math.min(
      30_000,
      1_000 * 2 ** Math.min(this.reconcileRetryAttempt - 1, 5),
    );
    this.reconcileRetryTimeoutId = window.setTimeout(() => {
      this.reconcileRetryTimeoutId = undefined;
      void this.reconcile();
    }, delay);
  }

  private acceptCheckpoint(checkpoint: StateSyncCheckpoint): void {
    for (const [scope, revision] of Object.entries(
      checkpoint.revisions ?? {},
    )) {
      this.acceptRevision(scope, revision);
    }
  }

  private acceptRevision(scope: StateSyncScope, revision: number): void {
    if (!Number.isSafeInteger(revision) || revision < 0) return;
    (
      this.injector.get(
        DomainVersionTracker,
        null,
      ) as Partial<DomainVersionTracker> | null
    )?.observe?.({ [scope]: revision });
    const current = this.revisions.get(scope) ?? 0;
    if (revision > current) this.revisions.set(scope, revision);

    if (revision >= current && this.hasStaleRegistration(scope, revision)) {
      this.scheduleRefresh(scope);
    }
  }

  private scheduleRefresh(scope: StateSyncScope): void {
    const existing = this.pendingRefreshes.get(scope);
    if (existing !== undefined) window.clearTimeout(existing);

    const timeoutId = window.setTimeout(() => {
      this.pendingRefreshes.delete(scope);
      const revision = this.revisions.get(scope) ?? 0;
      const registrations = this.registrations.get(scope);
      if (!registrations) return;

      for (const registration of registrations.values()) {
        this.refreshRegistration(scope, registration, revision);
      }
    }, 50);
    this.pendingRefreshes.set(scope, timeoutId);
  }

  acceptMutationResponse(
    revisions: Record<string, number>,
    forceRefresh = true,
    scopesHandledByResponse: readonly StateSyncScope[] = [],
  ): void {
    const handledScopes = new Set(scopesHandledByResponse);
    for (const [scope, revision] of Object.entries(revisions)) {
      if (!Number.isSafeInteger(revision) || revision < 1) continue;
      const responseWasRejected =
        this.rejectedMutationRevisions.get(scope) === revision;
      if (responseWasRejected) {
        this.rejectedMutationRevisions.delete(scope);
      }
      if (handledScopes.has(scope) && !responseWasRejected) {
        this.acceptHandledMutationRevision(scope, revision);
        continue;
      }
      if (!forceRefresh && !responseWasRejected) {
        this.acceptRevision(scope, revision);
        continue;
      }

      const current = this.revisions.get(scope) ?? 0;
      const targetRevision = Math.max(current, revision);
      this.revisions.set(scope, targetRevision);
      const registrations = this.registrations.get(scope);
      if (!registrations) continue;

      for (const registration of registrations.values()) {
        registration.lastRefreshRevision = Math.min(
          registration.lastRefreshRevision,
          targetRevision - 1,
        );
      }
      this.scheduleRefresh(scope);
    }
    this.publishStatus();
  }

  private acceptHandledMutationRevision(
    scope: StateSyncScope,
    revision: number,
  ): void {
    (
      this.injector.get(
        DomainVersionTracker,
        null,
      ) as Partial<DomainVersionTracker> | null
    )?.observe?.({ [scope]: revision });
    const current = this.revisions.get(scope) ?? 0;
    const targetRevision = Math.max(current, revision);
    this.revisions.set(scope, targetRevision);

    const registrations = this.registrations.get(scope);
    if (registrations) {
      for (const registration of registrations.values()) {
        registration.lastRefreshRevision = Math.max(
          registration.lastRefreshRevision,
          revision,
        );
      }
    }

    if (this.hasStaleRegistration(scope, targetRevision)) {
      this.scheduleRefresh(scope);
      return;
    }

    const pendingRefresh = this.pendingRefreshes.get(scope);
    if (pendingRefresh !== undefined) {
      window.clearTimeout(pendingRefresh);
      this.pendingRefreshes.delete(scope);
    }
  }

  private refreshRegistration(
    scope: StateSyncScope,
    registration: StateSyncRegistration,
    revision: number,
  ): void {
    if (
      registration.inFlight ||
      registration.lastRefreshRevision >= revision ||
      !untracked(registration.shouldRefresh)
    ) {
      return;
    }

    if (registration.retryTimeoutId !== undefined) {
      window.clearTimeout(registration.retryTimeoutId);
      registration.retryTimeoutId = undefined;
    }
    registration.inFlight = true;
    const generation = registration.generation;
    registration.lastError = undefined;
    const diagnostics = this.injector.get(
      StateSyncDiagnostics,
      null,
    ) as Partial<StateSyncDiagnostics> | null;
    diagnostics?.recordRefresh?.(scope, registration.key);
    this.publishStatus();

    let result: StateSyncRefreshResult;
    try {
      result = untracked(registration.refresh);
    } catch (error) {
      this.handleRefreshFailure(scope, registration, error);
      return;
    }

    const completion = isObservable(result)
      ? firstValueFrom(result as Observable<unknown>)
      : Promise.resolve(result);

    void completion.then(
      () => {
        if (registration.generation !== generation) return;
        registration.inFlight = false;
        registration.retryAttempt = 0;
        registration.lastError = undefined;
        registration.lastRefreshRevision = Math.max(
          registration.lastRefreshRevision,
          revision,
        );
        this.publishStatus();

        const currentRevision = this.revisions.get(scope) ?? 0;
        if (registration.lastRefreshRevision < currentRevision) {
          this.scheduleRefresh(scope);
        }
      },
      (error) => {
        if (registration.generation !== generation) return;
        this.handleRefreshFailure(scope, registration, error);
      },
    );
  }

  private handleRefreshFailure(
    scope: StateSyncScope,
    registration: StateSyncRegistration,
    error: unknown,
  ): void {
    registration.inFlight = false;
    registration.lastError = error;
    registration.retryAttempt += 1;
    this.publishStatus();
    console.warn(
      `State synchronization failed for ${registration.key}; retrying`,
      error,
    );

    const delay = Math.min(
      30_000,
      1_000 * 2 ** Math.min(registration.retryAttempt - 1, 5),
    );
    registration.retryTimeoutId = window.setTimeout(() => {
      registration.retryTimeoutId = undefined;
      const revision = this.revisions.get(scope) ?? 0;
      this.refreshRegistration(scope, registration, revision);
    }, delay);
  }

  private hasStaleRegistration(
    scope: StateSyncScope,
    revision: number,
  ): boolean {
    const registrations = this.registrations.get(scope);
    return (
      registrations !== undefined &&
      [...registrations.values()].some(
        (registration) => registration.lastRefreshRevision < revision,
      )
    );
  }

  private publishStatus(): void {
    const status: StateSyncRegistrationStatus[] = [];
    for (const [scope, registrations] of this.registrations) {
      const targetRevision = this.revisions.get(scope) ?? 0;
      for (const registration of registrations.values()) {
        status.push({
          scope,
          key: registration.key,
          targetRevision,
          appliedRevision: registration.lastRefreshRevision,
          stale: registration.lastRefreshRevision < targetRevision,
          refreshing: registration.inFlight,
          retryAttempt: registration.retryAttempt,
          lastError: registration.lastError ?? null,
        });
      }
    }
    this._status.set(status);
  }

  private readonly handleBlur = (): void => {
    this.lastBlurAt = Date.now();
  };

  private readonly handleFocus = (): void => {
    const lastBlurAt = this.lastBlurAt;
    this.lastBlurAt = undefined;
    if (
      lastBlurAt === undefined ||
      Date.now() - lastBlurAt < this.longSuspensionMs
    ) {
      return;
    }
    void this.reconcile();
  };

  private readonly handleOnline = (): void => {
    void this.reconcile();
  };
}
