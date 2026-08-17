import { Injectable, Injector, untracked } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { StateSyncService } from '../../api/state-sync/state-sync.service';
import {
  StateInvalidated,
  StateSyncCheckpoint,
  StateSyncScope,
} from './game-realtime-contracts';

interface StateSyncRegistration {
  key: string;
  refresh: () => void | Promise<void>;
  shouldRefresh: () => boolean;
  lastRefreshRevision: number;
}

@Injectable({ providedIn: 'root' })
export class StateSyncCoordinator {
  private readonly registrations = new Map<
    StateSyncScope,
    Map<string, StateSyncRegistration>
  >();
  private readonly revisions = new Map<StateSyncScope, number>();
  private readonly pendingRefreshes = new Map<StateSyncScope, number>();
  private readonly handledUpdateIds = new Set<string>();
  private readonly handledUpdateOrder: string[] = [];
  private reconcilePromise?: Promise<void>;
  private initialized = false;
  private lastFocusReconcileAt = 0;

  constructor(private readonly injector: Injector) {}

  initialize(): void {
    if (this.initialized) return;
    this.initialized = true;
    window.addEventListener('focus', this.handleFocus);
    window.addEventListener('online', this.handleOnline);
  }

  dispose(): void {
    if (this.initialized) {
      window.removeEventListener('focus', this.handleFocus);
      window.removeEventListener('online', this.handleOnline);
    }
    this.initialized = false;
    this.revisions.clear();
    this.handledUpdateIds.clear();
    this.handledUpdateOrder.length = 0;
    for (const timeoutId of this.pendingRefreshes.values()) {
      window.clearTimeout(timeoutId);
    }
    this.pendingRefreshes.clear();
    for (const scopedRegistrations of this.registrations.values()) {
      for (const registration of scopedRegistrations.values()) {
        registration.lastRefreshRevision = 0;
      }
    }
  }

  register(
    scope: StateSyncScope,
    key: string,
    refresh: () => void | Promise<void>,
    shouldRefresh: () => boolean = () => true,
  ): () => void {
    let scoped = this.registrations.get(scope);
    if (!scoped) {
      scoped = new Map<string, StateSyncRegistration>();
      this.registrations.set(scope, scoped);
    }

    scoped.set(key, {
      key,
      refresh,
      shouldRefresh,
      lastRefreshRevision: this.revisions.get(scope) ?? 0,
    });

    return () => scoped?.delete(key);
  }

  acceptInvalidation(event: StateInvalidated, updateId?: string): void {
    if (updateId && this.isDuplicate(updateId)) return;
    this.acceptRevision(event.scope, event.revision);
  }

  reconcile(): Promise<void> {
    if (this.reconcilePromise) return this.reconcilePromise;

    const service = this.injector.get(StateSyncService);
    this.reconcilePromise = firstValueFrom(service.getCheckpoint())
      .then((checkpoint) => this.acceptCheckpoint(checkpoint))
      .catch((error) => {
        console.warn('State checkpoint reconciliation failed', error);
      })
      .finally(() => {
        this.reconcilePromise = undefined;
      });
    return this.reconcilePromise;
  }

  private acceptCheckpoint(checkpoint: StateSyncCheckpoint): void {
    for (const [scope, revision] of Object.entries(checkpoint.revisions ?? {})) {
      this.acceptRevision(scope, revision);
    }
  }

  private acceptRevision(scope: StateSyncScope, revision: number): void {
    if (!Number.isSafeInteger(revision) || revision < 0) return;
    const current = this.revisions.get(scope) ?? 0;
    if (revision <= current) return;

    this.revisions.set(scope, revision);
    this.scheduleRefresh(scope);
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
        if (registration.lastRefreshRevision >= revision) continue;
        registration.lastRefreshRevision = revision;
        if (!untracked(registration.shouldRefresh)) continue;

        try {
          void Promise.resolve(untracked(registration.refresh)).catch((error) =>
            console.warn(
              `State synchronization failed for ${registration.key}`,
              error,
            ),
          );
        } catch (error) {
          console.warn(
            `State synchronization failed for ${registration.key}`,
            error,
          );
        }
      }
    }, 50);
    this.pendingRefreshes.set(scope, timeoutId);
  }

  private isDuplicate(updateId: string): boolean {
    if (this.handledUpdateIds.has(updateId)) return true;
    this.handledUpdateIds.add(updateId);
    this.handledUpdateOrder.push(updateId);
    while (this.handledUpdateOrder.length > 1_000) {
      const oldest = this.handledUpdateOrder.shift();
      if (oldest) this.handledUpdateIds.delete(oldest);
    }
    return false;
  }

  private readonly handleFocus = (): void => {
    const now = Date.now();
    if (now - this.lastFocusReconcileAt < 5_000) return;
    this.lastFocusReconcileAt = now;
    void this.reconcile();
  };

  private readonly handleOnline = (): void => {
    void this.reconcile();
  };
}
