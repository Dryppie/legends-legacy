import { computed, effect, Injectable, signal } from '@angular/core';
import {
  catchError,
  finalize,
  Observable,
  of,
  shareReplay,
  tap,
  throwError,
} from 'rxjs';
import { CharacterActionsStateService } from '../character-actions/character-actions.state.service';
import { AuthService } from '../auth/auth.service';
import {
  GameBootstrapDto,
  GameBootstrapService,
} from './game-bootstrap.service';
import { QuestStateService } from '../quest/quest-state.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { setAttributeDefinitions } from '../../../../shared/models/attribute-definition';
import { TimeSyncService } from '../time-sync/time-sync.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { StateSyncScope } from '../../real-time/game-realtime/game-realtime-contracts';

@Injectable({ providedIn: 'root' })
export class GameBootstrapStateService {
  private readonly _bootstrap = signal<GameBootstrapDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _loaded = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _serverTimeUtc = signal<string | null>(null);
  private inFlight$?: Observable<GameBootstrapDto | null>;

  readonly bootstrap = computed(() => this._bootstrap());
  readonly loading = computed(() => this._loading());
  readonly loaded = computed(() => this._loaded());
  readonly error = computed(() => this._error());
  readonly serverTimeUtc = computed(() => this._serverTimeUtc());
  readonly accountAccess = computed(() => this._bootstrap()?.accountAccess ?? null);
  readonly canParticipate = computed(
    () => this.accountAccess()?.canParticipate ?? true,
  );

  constructor(
    private readonly bootstrapService: GameBootstrapService,
    private readonly auth: AuthService,
    private readonly questState: QuestStateService,
    private readonly characterActionsState: CharacterActionsStateService,
    private readonly gameEvents: GameRealtimeEventRegistry,
    private readonly timeSync: TimeSyncService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    effect(
      () => {
        if (!this.auth.isAuthenticated()) {
          this.reset();
          return;
        }

        const accessChanged = this.gameEvents.event.AccountAccessChanged();
        if (!this._loaded() || !accessChanged) {
          return;
        }

        queueMicrotask(() => {
          this.reload().subscribe({
            error: () => undefined,
          });
        });
      },
    );
  }

  load(): Observable<GameBootstrapDto | null> {
    return this.fetch(false);
  }

  reload(): Observable<GameBootstrapDto | null> {
    return this.fetch(true);
  }

  reset(): void {
    this.inFlight$ = undefined;
    this._bootstrap.set(null);
    this._loading.set(false);
    this._loaded.set(false);
    this._error.set(null);
    this._serverTimeUtc.set(null);
    setAttributeDefinitions([]);
  }

  private fetch(force: boolean): Observable<GameBootstrapDto | null> {
    if (!this.auth.isAuthenticated()) {
      this.reset();
      return of(null);
    }

    if (!force && this.canUseCachedBootstrap()) {
      return of(this._bootstrap());
    }

    if (this.inFlight$) {
      return this.inFlight$;
    }

    this._loading.set(true);
    this._error.set(null);

    this.inFlight$ = this.bootstrapService.get().pipe(
      tap((bootstrap) => this.hydrate(bootstrap)),
      catchError((err) => {
        this._error.set(err?.message ?? 'Failed to load game state');
        return throwError(() => err);
      }),
      finalize(() => {
        this._loading.set(false);
        this.inFlight$ = undefined;
      }),
      shareReplay(1),
    );

    return this.inFlight$;
  }

  private hydrate(bootstrap: GameBootstrapDto): void {
    this._bootstrap.set(bootstrap);
    this._serverTimeUtc.set(bootstrap.serverTimeUtc);
    this.timeSync.updateFromServerTime(bootstrap.serverTimeUtc);
    setAttributeDefinitions(bootstrap.attributeDefinitions);
    const versions = bootstrap.stateVersions ?? {};
    const acceptedScopes: StateSyncScope[] = [];
    if (this.isCurrentSnapshot('character', versions['character'])) {
      this.auth.updateCharacter(bootstrap.character);
      acceptedScopes.push('character');
    }
    if (this.isCurrentSnapshot('quests', versions['quests'])) {
      this.questState.initialize(bootstrap.questJournal);
      acceptedScopes.push('quests');
    }
    if (this.isCurrentSnapshot('area-access', versions['area-access'])) {
      this.questState.initializeAreaAccess(bootstrap.areaAccess);
      acceptedScopes.push('area-access');
    }
    this.characterActionsState.initializeFromBootstrap(bootstrap.currentAction);
    this.stateSync.acceptSnapshotResponse(versions, acceptedScopes);
    this._loaded.set(true);
  }

  private isCurrentSnapshot(scope: StateSyncScope, revision: number | undefined): boolean {
    return revision === undefined || this.domainVersions.isCurrent(scope, revision);
  }

  private canUseCachedBootstrap(): boolean {
    const bootstrap = this._bootstrap();
    const currentCharacter = this.auth.currentCharacter();

    return (
      this._loaded() &&
      !!bootstrap &&
      !!currentCharacter &&
      bootstrap.character.id === currentCharacter.id
    );
  }
}
