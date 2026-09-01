import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { finalize, Observable, of, tap } from 'rxjs';
import {
  CharacterOverviewDto,
  CharacterDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';
import { CharacterService } from './character.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { CharacterLevelUp } from '../../real-time/game-realtime/game-realtime-contracts';

export function applyCharacterLevelUp(
  character: CharacterDto,
  levelUp: CharacterLevelUp,
): CharacterDto {
  if (character.id !== levelUp.characterId || levelUp.level < character.level) {
    return character;
  }

  if (
    levelUp.level === character.level &&
    levelUp.experience <= character.experience
  ) {
    return character;
  }

  return {
    ...character,
    level: levelUp.level,
    experience: levelUp.experience,
    experienceUntilNextLevel: levelUp.experienceUntilNextLevel,
  };
}

@Injectable({ providedIn: 'root' })
export class CharacterStateService {
  /* ─────────── writable signals ─────────── */
  private readonly _overview = signal<CharacterOverviewDto | null>(null);
  private readonly _overviewDirty = signal(false);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
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
    private readonly stateSync: StateSyncCoordinator,
    private readonly router: Router,
    private readonly domainVersions: DomainVersionTracker,
    private readonly gameEvents: GameRealtimeEventRegistry,
  ) {
    this.stateSync.register(
      'character',
      'character-summary',
      () => this.synchronizeCharacterSummary(),
      () => !!this.currentCharacterId(),
      true,
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
        this.stateSync.activate('character', 'character-summary');
        this.stateSync.activate('character-overview', 'character-overview');
        this.markOverviewDirty();
        if (this.isOverviewRouteActive()) {
          this.refresh(); // writes _loading, _overview
        }
      },
    );

    effect(
      () => {
        const levelUp = this.gameEvents.event.CharacterLevelUp();
        if (!levelUp) return;

        untracked(() => {
          const current = this.currentCharacter();
          if (!current) return;

          const updated = applyCharacterLevelUp(current, levelUp);
          if (updated === current) return;

          this.updateCharacter(updated);
          this.patchOverviewFromSummary(updated);
        });
      },
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
    return this.auth
      .refreshCurrentCharacterRequest()
      .pipe(tap((character) => this.patchOverviewFromSummary(character)));
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

  applyVersionedCharacter<T extends { character: CharacterDto }>(
    result: VersionedMutationResult<T>,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'character',
        result.domainVersions['character'],
      )
    ) {
      return false;
    }

    this.updateCharacter(result.data.character);
    return true;
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
}
