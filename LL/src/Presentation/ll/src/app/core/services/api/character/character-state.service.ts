import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Observable, of, tap } from 'rxjs';
import {
  CharacterOverviewDto,
  CharacterDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';
import { CharacterService } from './character.service';
import { GameEventService } from '../../real-time/game-event.service';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';

@Injectable({ providedIn: 'root' })
export class CharacterStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _overview = signal<CharacterOverviewDto | null>(null);
  private readonly _overviewDirty = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly eventDeduper = new GameEventDeduper();
  private dirtyVersion = 0;
  private activeRefreshDirtyVersion: number | null = null;
  private refreshAfterCurrentRequest = false;
  private overviewRequestEpoch = 0;

  /* ─────────── public, read-only selectors ─────────── */
  /** Current character comes straight from AuthService, no copy needed */
  readonly currentCharacter = computed(() => this.auth.currentCharacter());
  readonly currentCharacterId = computed(
    () => this.currentCharacter()?.id ?? null,
  );

  readonly overview = computed(() => this._overview());
  readonly overviewDirty = computed(() => this._overviewDirty());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly hasData = computed(() => !!this._overview());

  constructor(
    private readonly service: CharacterService,
    private readonly auth: AuthService,
    private readonly eventService: GameEventService,
    private readonly stateSync: StateSyncCoordinator,
    private readonly router: Router,
  ) {
    this.stateSync.register(
      'character',
      'character-summary',
      () => this.synchronizeCharacterSummary(),
      () => !!this.currentCharacterId(),
    );
    this.stateSync.register(
      'character-overview',
      'character-overview',
      () => this.handleOverviewInvalidation(),
      () => !!this.currentCharacterId(),
    );
    /* load (or clear) overview whenever the selected character changes */
    effect(
      () => {
        const id = this.currentCharacterId();
        if (!id) {
          this._overview.set(null);
          this._overviewDirty.set(false);
          this.dirtyVersion = 0;
          return;
        }
        this.markOverviewDirty();
        if (this.isOverviewRouteActive()) {
          this.refresh(); // writes _loading, _overview
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const characterId = this.currentCharacterId();
        const soulstoneDropEnvelope =
          this.eventService.eventEnvelope.SoulstoneDropMsg();
        const levelUpEnvelope =
          this.eventService.eventEnvelope.CharacterLevelUpMsg();
        const soulstoneDrop = soulstoneDropEnvelope?.payload;
        const levelUp = levelUpEnvelope?.payload;

        if (
          characterId &&
          soulstoneDrop &&
          this.eventDeduper.shouldProcess(
            'soulstone-drop',
            soulstoneDropEnvelope,
          ) &&
          soulstoneDrop.characterId === characterId
        ) {
          this.updateCurrentCharacter({
            soulstones: soulstoneDrop.totalSoulstones,
          });
        }

        if (
          characterId &&
          levelUp &&
          this.eventDeduper.shouldProcess('level-up', levelUpEnvelope) &&
          levelUp.characterId === characterId
        ) {
          this.updateCurrentCharacter({
            level: levelUp.level,
            experience: levelUp.experience,
            experienceUntilNextLevel: levelUp.experienceUntilNextLevel,
          });
          this.markOverviewDirty();
          if (this.isOverviewRouteActive()) this.refreshIfDirty();
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount > 0) {
          this.auth.refreshCurrentCharacter();
          this.markOverviewDirty();
          if (this.isOverviewRouteActive()) this.refreshIfDirty();
        }
      },
      { allowSignalWrites: true },
    );
  }

  /* ─────────── API calls / mutations ─────────── */

  /** Get the latest overview from the backend */
  refresh(): void {
    if (!untracked(() => this.currentCharacterId())) return; // nothing to load
    if (untracked(() => this._loading())) {
      this.refreshAfterCurrentRequest = true;
      return;
    }

    this._loading.set(true);
    this.refreshAfterCurrentRequest = false;
    const requestDirtyVersion = this.dirtyVersion;
    const requestEpoch = ++this.overviewRequestEpoch;
    this.activeRefreshDirtyVersion = requestDirtyVersion;

    this.service
      .getCharacterOverview()
      .pipe(
        finalize(() => {
          if (requestEpoch !== this.overviewRequestEpoch) return;
          this._loading.set(false);
          this.activeRefreshDirtyVersion = null;
          if (this.refreshAfterCurrentRequest) {
            this.refreshAfterCurrentRequest = false;
            this.refresh();
          }
        }),
      )
      .subscribe({
        next: (ov) => {
          if (requestEpoch !== this.overviewRequestEpoch) return;
          this._overview.set(ov);
          this._error.set(null);
          if (requestDirtyVersion === this.dirtyVersion) {
            this._overviewDirty.set(false);
          }
        },
        error: (e) => {
          if (requestEpoch !== this.overviewRequestEpoch) return;
          this._overviewDirty.set(true);
          this._error.set(e.message ?? 'Failed to load character');
        },
      });
  }

  markOverviewDirty(): void {
    this.dirtyVersion += 1;
    this._overviewDirty.set(true);
  }

  refreshIfDirty(): void {
    const needsRefresh = untracked(
      () => this._overviewDirty() || !this._overview(),
    );
    if (!needsRefresh) return;

    if (untracked(() => this._loading())) {
      if (this.activeRefreshDirtyVersion !== this.dirtyVersion) {
        this.refreshAfterCurrentRequest = true;
      }
      return;
    }
    this.refresh();
  }

  refreshCurrentCharacter(): void {
    this.auth.refreshCurrentCharacter();
    this.refresh();
  }

  private synchronizeCharacterSummary(): Observable<unknown> {
    return this.auth.refreshCurrentCharacterRequest().pipe(
      tap((character) => this.patchOverviewFromSummary(character)),
    );
  }

  private handleOverviewInvalidation(): Observable<unknown> {
    this.markOverviewDirty();
    return this.isOverviewRouteActive()
      ? this.synchronizeOverview()
      : of(undefined);
  }

  private synchronizeOverview(): Observable<unknown> {
    const requestDirtyVersion = this.dirtyVersion;
    const requestEpoch = ++this.overviewRequestEpoch;
    this._loading.set(true);
    this._error.set(null);

    return this.service.getCharacterOverview().pipe(
      tap({
        next: (overview) => {
          if (requestEpoch !== this.overviewRequestEpoch) return;
          this._overview.set(overview);
          if (requestDirtyVersion === this.dirtyVersion) {
            this._overviewDirty.set(false);
          }
        },
        error: (error) => {
          if (requestEpoch !== this.overviewRequestEpoch) return;
          this._overviewDirty.set(true);
          this._error.set(error?.message ?? 'Failed to load character');
        },
      }),
      finalize(() => {
        if (requestEpoch === this.overviewRequestEpoch) {
          this._loading.set(false);
        }
      }),
    );
  }

  private patchOverviewFromSummary(character: CharacterDto): void {
    const overview = this._overview();
    if (!overview || overview.id !== character.id) return;

    this._overview.set({
      ...overview,
      name: character.name,
      level: character.level,
      experience: character.experience,
      experienceUntilNextLevel: character.experienceUntilNextLevel,
      equippedTitle: character.equippedTitle,
      isOnline: true,
      lastSeenAt: new Date().toISOString(),
    });
  }

  private isOverviewRouteActive(): boolean {
    return this.router.url
      .split(/[?#]/, 1)[0]
      .endsWith('/character/character-overview');
  }

  /** Optimistic cache update (optional helper) */
  setOverview(ov: CharacterOverviewDto): void {
    this._overview.set(ov);
    this._overviewDirty.set(false);
  }

  /** Forward the change to AuthService */
  updateCharacter(updated: CharacterDto): void {
    this.auth.updateCharacter(updated);
  }

  updateEquippedTitle(equippedTitle: CharacterDto['equippedTitle']): void {
    const character = this.currentCharacter();
    if (!character) return;

    this.auth.updateCharacter({
      ...character,
      equippedTitle,
    });

    const overview = this._overview();
    if (!overview) return;

    this._overview.set({
      ...overview,
      equippedTitle,
    });
  }

  private updateCurrentCharacter(patch: Partial<CharacterDto>): void {
    const character = this.currentCharacter();
    if (!character) return;

    this.auth.updateCharacter({
      ...character,
      ...patch,
    });
  }
}
