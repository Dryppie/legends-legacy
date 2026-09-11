import { computed, DestroyRef, effect, Injectable, signal, untracked } from '@angular/core';
import { catchError, filter, finalize, map, Observable, of, shareReplay, switchMap, tap, throwError } from 'rxjs';
import { ApiService } from '../api.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { TimeSyncService } from '../time-sync/time-sync.service';
import { StateSyncScope } from '../../real-time/game-realtime/game-realtime-contracts';

export interface NobilityStatus {
  isNoble: boolean; serverTime: string; expiresAt: string | null;
  membershipVersion: string; availableSignets: number; listedSignets: number;
  hasSupportHistory: boolean; showBadge: boolean;
  dailyRewardsThrough: string | null;
  benefits: { offlineHours: number; essenceLoadouts: number; equipmentLoadouts: number;
    arenaTicketCap: number; focusCooldownHours: number; marketSellLimit: number; marketBuyLimit: number;
    freeProphecyRerolls: number; totalProphecyRerolls: number; experienceBonusBps: number };
}
export interface SignetPreview { membershipVersion: string; unitIds: string[]; expiresAt: string; expiryIsEstimate: boolean }
interface SignetRedemptionRequest { operationId: string; membershipVersion: string; unitIds: string[]; expectedExpiryDate: string }
export interface NobilityAppearance { serverTime: string; expiresAt: string | null; showBadge: boolean }

@Injectable({ providedIn: 'root' })
export class NobilityService {
  readonly status = signal<NobilityStatus | null>(null);
  private readonly pendingRedemption = signal<SignetRedemptionRequest | null>(null);
  readonly pendingQuantity = computed(() => this.pendingRedemption()?.unitIds.length ?? null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly isNoble = computed(() => this.status()?.isNoble ?? false);
  readonly equipmentLimit = computed(() => this.isNoble() ? 6 : 3);
  private expiryTimer?: ReturnType<typeof setTimeout>;
  private epoch = 0;
  private readonly appearances = new Map<string, { until: number; value: Observable<NobilityAppearance> }>();

  constructor(private api: ApiService, private sync: StateSyncCoordinator,
    private events: EventBusService, private time: TimeSyncService, destroy: DestroyRef) {
    sync.register('nobility', 'nobility', () => this.refresh());
    effect(() => { if (events.logout()) untracked(() => {
      this.epoch++; this.status.set(null); this.pendingRedemption.set(null);
      this.busy.set(false); this.error.set(null); this.message.set(null); this.appearances.clear();
      clearTimeout(this.expiryTimer);
    }); });
    if (typeof window !== 'undefined') {
      const refresh = () => { if (this.status()) this.refresh().subscribe({ error: () => undefined }); };
      window.addEventListener('focus', refresh);
      window.addEventListener('online', refresh);
      destroy.onDestroy(() => { window.removeEventListener('focus', refresh); window.removeEventListener('online', refresh); clearTimeout(this.expiryTimer); });
    }
  }

  hydrate(status: NobilityStatus): void {
    if (this.status() && Date.parse(status.serverTime) < Date.parse(this.status()!.serverTime)) return;
    const membershipChanged = this.status() !== null && this.isNoble() !== status.isNoble;
    this.time.updateFromServerTime(status.serverTime);
    this.status.set(status);
    if (membershipChanged) this.refreshGameplay();
    clearTimeout(this.expiryTimer);
    if (status.isNoble && status.expiresAt) {
      const delay = Math.max(1, Date.parse(status.expiresAt) - this.time.now());
      this.expiryTimer = setTimeout(() => {
        if (Date.parse(status.expiresAt!) <= this.time.now()) {
          this.status.update(value => value ? { ...value, isNoble: false } : value);
          this.refreshGameplay();
          this.reconcile();
        } else this.refresh().subscribe({ error: () => undefined });
      }, Math.min(delay, 2_147_000_000));
    }
  }

  private refreshGameplay(): void {
    // Expiry changes effective policies even if no database row was written.
    // Re-fetch at the known revisions; never invent a server revision.
    const scopes: StateSyncScope[] = ['essences', 'equipment', 'colosseum', 'prophecies'];
    const revisions: Partial<Record<StateSyncScope, number>> = {};
    for (const scope of scopes) {
      const revision = this.sync.latestRevision(scope);
      if (revision > 0) revisions[scope] = revision;
    }
    this.sync.acceptMutationResponse(revisions);
  }

  refresh(): Observable<NobilityStatus> {
    const epoch = this.epoch;
    return this.api.get('nobility').pipe(tap(status => { if (epoch === this.epoch) this.hydrate(status); }));
  }

  reconcile(): void {
    this.run(this.post<NobilityStatus>('nobility/reconcile', {}).pipe(tap(status => this.hydrate(status))));
  }

  redeem(quantity: number): void {
    if (this.busy()) return;
    const epoch = this.epoch;
    const pending = this.pendingRedemption();
    if (!pending && (!Number.isInteger(quantity) || quantity < 1 || quantity > 1200 ||
      quantity > (this.status()?.availableSignets ?? 0))) return;

    // Resolve the server's exact units and expiry internally, then redeem in one action.
    // An uncertain response must retry the same operation, even if inventory refreshed.
    const request = pending ? of(pending) : this.post<SignetPreview>('nobility/redemption-preview', { quantity }).pipe(
      map(preview => ({ operationId: crypto.randomUUID(), membershipVersion: preview.membershipVersion,
        unitIds: preview.unitIds, expectedExpiryDate: preview.expiresAt.slice(0, 10) })),
      tap(value => this.pendingRedemption.set(value)),
    );
    this.run(request.pipe(
      switchMap(value => this.post<{ expiresAt: string }>('nobility/redeem', value)),
      catchError(error => {
        if (epoch === this.epoch && (error?.status === 400 || error?.status === 409)) this.pendingRedemption.set(null);
        return throwError(() => error);
      }),
      tap(response => {
        this.pendingRedemption.set(null); this.appearances.clear();
        this.message.set('Signets redeemed. Nobility expires ' + new Date(response.expiresAt).toLocaleString() + '.');
      }), switchMap(() => this.refresh())));
  }

  appearance(showBadge: boolean): void {
    const epoch = this.epoch;
    this.run(this.api.put('nobility/appearance', { showBadge }).pipe(
      filter(() => epoch === this.epoch),
      tap(() => this.appearances.clear()),
      switchMap(() => this.refresh())));
  }

  publicAppearance(characterId: string): Observable<NobilityAppearance> {
    const cached = this.appearances.get(characterId);
    if (cached && cached.until > Date.now()) return cached.value;
    const value: Observable<NobilityAppearance> = this.api.get('nobility/characters/' + encodeURIComponent(characterId)).pipe(
      tap((appearance: NobilityAppearance) => this.time.updateFromServerTime(appearance.serverTime)),
      catchError(() => of({ serverTime: new Date().toISOString(), expiresAt: null, showBadge: false })), shareReplay(1));
    if (this.appearances.size >= 512) this.appearances.delete(this.appearances.keys().next().value!);
    this.appearances.set(characterId, { until: Date.now() + 60_000, value });
    return value;
  }

  private run(operation: Observable<unknown>): void {
    if (this.busy()) return;
    const epoch = this.epoch;
    this.busy.set(true); this.error.set(null); this.message.set(null);
    operation.pipe(finalize(() => { if (epoch === this.epoch) this.busy.set(false); })).subscribe({
      error: error => { if (epoch === this.epoch) {
        this.error.set(error?.error?.errorMessage ?? error?.errorMessage ?? error?.message ?? 'Nobility could not be updated. Try again.');
      } },
    });
  }

  // ResponseResultFilter returns the payload on success; failures arrive as HTTP errors.
  private post<T>(path: string, body: object): Observable<T> {
    const epoch = this.epoch;
    return this.api.post(path, body).pipe(filter(() => epoch === this.epoch));
  }
}

