import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { finalize } from 'rxjs';
import {
  CharacterOverviewDto,
  CharacterDto,
} from '../../../../shared/models/Dtos/characterDto';
import { AuthService } from '../auth/auth.service';
import { CharacterService } from './character.service';
import { GameEventService } from '../../real-time/game-event.service';
import { GameEventDeduper } from '../../real-time/game-event/game-event-consumer';

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
  ) {
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
        this.refresh(); // writes _loading, _overview
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
          this.refresh();
        }
      },
      { allowSignalWrites: true },
    );

    effect(
      () => {
        const reconnectCount = this.eventService.reconnectCount();
        if (reconnectCount > 0) {
          this.auth.refreshCurrentCharacter();
          this.refresh();
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
    this.activeRefreshDirtyVersion = requestDirtyVersion;

    this.service
      .getCharacterOverview()
      .pipe(
        finalize(() => {
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
          this._overview.set(ov);
          this._error.set(null);
          if (requestDirtyVersion === this.dirtyVersion) {
            this._overviewDirty.set(false);
          }
        },
        error: (e) => {
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
