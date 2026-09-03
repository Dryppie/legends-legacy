import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import {
  EMPTY,
  concatMap,
  forkJoin,
  from,
  last,
  Observable,
  of,
  catchError,
  finalize,
  map,
  takeWhile,
  tap,
} from 'rxjs';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { EssencesService } from './essences.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CharacterStateService } from '../character/character-state.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { RealtimeSignalDeduper } from '../../real-time/game-realtime/realtime-deduplication';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { EquipmentStateService } from '../equipment/equipment-state.service';
import { VersionedMutationResult } from '../api.service';
import { Essence } from '../../../../shared/models/essence';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import {
  EssenceItem,
  inferEssenceDefinitionId,
} from '../../../../shared/models/item';
import {
  CreatureArchiveDto,
  EssenceCombatActivity,
  EssenceCodexDto,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  EssenceMutationResponseDto,
  EssenceStateResponseDto,
  PlayerEssenceDto,
  SaveEssenceLoadoutSlotDto,
  SoulArchiveDto,
} from '../../../../shared/models/essence-system';

export type EssenceView = 'archive' | 'absorb' | 'creatures' | 'codex';
const NEW_LOADOUT_NAME = 'New Loadout';

@Injectable({ providedIn: 'root' })
export class EssenceStateService {
  private readonly _activeView = signal<EssenceView>('archive');
  private readonly _archive = signal<SoulArchiveDto | null>(null);
  private readonly _loadouts = signal<EssenceLoadoutsDto | null>(null);
  private readonly _creatureArchive = signal<CreatureArchiveDto | null>(null);
  private readonly _codex = signal<EssenceCodexDto | null>(null);
  private readonly _now = signal(Date.now());
  private readonly _seenEssenceFocusReadyKey = signal<string | null>(null);
  private readonly _highlightEssenceFocus = signal(false);
  private readonly _selectedPlayerEssenceId = signal<string | null>(null);
  private readonly _selectedLoadoutId = signal<string | null>(null);
  private readonly _selectedInventoryItemId = signal<string | null>(null);
  private readonly _draftLoadoutName = signal('Default');
  private readonly _draftSlots = signal<(string | null)[]>([]);
  private readonly _savingLoadout = signal(false);
  private readonly _loading = signal(false);
  private readonly _spendingDust = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _dirty = signal(true);
  private dirtyVersion = 0;
  private refreshedRevision = 0;
  private readonly eventDeduper = new RealtimeSignalDeduper();
  private resetVersion = 0;
  private dustMutationVersion = 0;
  private fullRefreshEpoch = 0;
  private archiveRequestEpoch = 0;
  private loadoutRequestEpoch = 0;
  private creatureArchiveRequestEpoch = 0;
  private codexRequestEpoch = 0;

  readonly activeView = computed(() => this._activeView());
  readonly currentTime = computed(() => this._now());
  readonly archive = computed(() => this._archive());
  readonly loadouts = computed(() => this._loadouts());
  readonly creatureArchive = computed(() => this._creatureArchive());
  readonly codex = computed(() => this._codex());
  readonly focusedCreature = computed(
    () =>
      this._creatureArchive()?.creatures.find(
        (creature) => creature.isEssenceFocus,
      ) ?? null,
  );
  readonly canChangeEssenceFocus = computed(() => {
    const archive = this._creatureArchive();
    if (!archive) return false;
    if (archive.canChangeEssenceFocus) return true;

    const availableAt = this.getUtcTime(archive.essenceFocusAvailableAtUtc);
    return availableAt !== null && availableAt <= this._now();
  });
  private readonly essenceFocusReadyKey = computed(() => {
    const archive = this._creatureArchive();
    if (!archive || !this.canChangeEssenceFocus()) return null;

    const hasSelectableTarget = archive.creatures.some(
      (creature) => creature.essences.length > 0 && !creature.isEssenceFocus,
    );
    if (!hasSelectableTarget) return null;

    return (
      archive.essenceFocusAvailableAtUtc ??
      archive.essenceFocusSetAtUtc ??
      'initial'
    );
  });
  readonly essenceFocusReady = computed(() => {
    const key = this.essenceFocusReadyKey();
    return key !== null && key !== this._seenEssenceFocusReadyKey();
  });
  readonly highlightEssenceFocus = computed(() =>
    this._highlightEssenceFocus(),
  );
  readonly selectedLoadoutId = computed(() => this._selectedLoadoutId());
  readonly draftLoadoutName = computed(() => this._draftLoadoutName());
  readonly draftSlots = computed(() => this._draftSlots());
  readonly savingLoadout = computed(() => this._savingLoadout());
  readonly loading = computed(() => this._loading());
  readonly spendingDust = computed(() => this._spendingDust());
  readonly error = computed(() => this._error());
  readonly dirty = this._dirty.asReadonly();

  readonly inventoryEssences = computed(() =>
    this.inventoryState
      .items()
      .filter(
        (item) => item.itemInstance.itemBase.itemType === ItemType.Essence,
      ),
  );

  readonly hasAbsorbableInventoryEssences = computed(() =>
    this.inventoryEssences().some(
      (item) => !this.isInventoryEssenceAbsorbed(item),
    ),
  );

  readonly absorbedEssenceDefinitionIds = computed(
    () =>
      new Set(
        this._archive()?.essences.map(
          (essence) => essence.essenceDefinitionId,
        ) ?? [],
      ),
  );

  readonly selected = computed<PlayerEssenceDto | null>(() => {
    const selectedId = this._selectedPlayerEssenceId();
    const essences = this._archive()?.essences ?? [];
    return (
      essences.find((essence) => essence.id === selectedId) ??
      essences[0] ??
      null
    );
  });

  readonly selectedLoadout = computed(() => {
    const selectedId = this._selectedLoadoutId();
    return (
      this._loadouts()?.loadouts.find((loadout) => loadout.id === selectedId) ??
      null
    );
  });

  readonly selectedInventoryItem = computed<InventoryItem | null>(() => {
    const selectedId = this._selectedInventoryItemId();
    return (
      this.inventoryEssences().find(
        (item) => item.itemInstance.id === selectedId,
      ) ?? null
    );
  });

  readonly selectedInventoryEssence = computed<Essence | null>(() => {
    const item = this.selectedInventoryItem();
    return item ? this.asEssence(item) : null;
  });

  readonly isSelectedInventoryEssenceAbsorbed = computed(() => {
    const item = this.selectedInventoryItem();
    return !!item && this.isInventoryEssenceAbsorbed(item);
  });

  readonly archiveText = computed(() => {
    const archive = this._archive();
    const loadouts = this._loadouts();
    const absorbed = archive?.essences.length ?? 0;
    const attuned =
      archive?.essences.filter(
        (essence) =>
          essence.attunedSlot !== null && essence.attunedSlot !== undefined,
      ).length ?? 0;
    if (!loadouts) {
      return `${absorbed} archived | loadouts loading`;
    }

    return `${absorbed} archived | ${attuned}/${loadouts?.unlockedSlots ?? 0} attuned`;
  });

  readonly slotIndexes = computed(() => {
    const unlockedSlots = this._loadouts()?.unlockedSlots ?? 0;
    return Array.from({ length: unlockedSlots }, (_, index) => index);
  });

  readonly essenceOptions = computed(() => this._archive()?.essences ?? []);

  readonly hasDuplicateDraftEssences = computed(() => {
    const essenceIds = this._draftSlots().filter(
      (essenceId): essenceId is string => !!essenceId,
    );
    return new Set(essenceIds).size !== essenceIds.length;
  });

  readonly hasDuplicateDraftCreatureSources = computed(() => {
    const creatureIds = this._draftSlots()
      .filter((playerEssenceId): playerEssenceId is string => !!playerEssenceId)
      .map((playerEssenceId) =>
        this.getCreatureIdForPlayerEssence(playerEssenceId),
      )
      .filter((creatureId): creatureId is string => !!creatureId);

    return new Set(creatureIds).size !== creatureIds.length;
  });

  readonly hasDraftChanges = computed(() => {
    if (!this._loadouts()) return false;

    const draftName = this._draftLoadoutName().trim();
    const draftSlots = this._draftSlots();
    const selectedLoadout = this.selectedLoadout();

    if (!selectedLoadout) {
      return (
        draftName !== NEW_LOADOUT_NAME ||
        draftSlots.some((essenceId) => !!essenceId)
      );
    }

    const savedSlots = this.getLoadoutDraftSlots(selectedLoadout);
    return (
      draftName !== selectedLoadout.name ||
      draftSlots.some(
        (essenceId, slotIndex) => essenceId !== savedSlots[slotIndex],
      )
    );
  });

  readonly hasDraftNameChanges = computed(() => {
    if (!this._loadouts()) return false;

    const draftName = this._draftLoadoutName().trim();
    const selectedLoadout = this.selectedLoadout();
    return selectedLoadout
      ? draftName !== selectedLoadout.name
      : draftName !== NEW_LOADOUT_NAME;
  });

  readonly canSaveDraft = computed(() => {
    const name = this._draftLoadoutName().trim();
    const loadouts = this._loadouts();
    const selectedId = this._selectedLoadoutId();
    if (!loadouts) return false;

    const canCreate = loadouts.loadouts.length < loadouts.limit;

    return (
      !!name &&
      this.hasDraftNameChanges() &&
      !this._savingLoadout() &&
      (!!selectedId || canCreate)
    );
  });

  constructor(
    private readonly essencesService: EssencesService,
    private readonly inventoryState: InventoryStateService,
    private readonly essenceItemView: EssenceItemViewService,
    private readonly eventBus: EventBusService,
    private readonly characterState: CharacterStateService,
    private readonly gameEvents: GameRealtimeEventRegistry,
    private readonly stateSync: StateSyncCoordinator,
    private readonly equipmentState: EquipmentStateService,
    private readonly domainVersions: DomainVersionTracker,
  ) {
    this.stateSync.register(
      'essences',
      'essences',
      ({ targetRevision }) => {
        if (targetRevision > this.refreshedRevision) this._dirty.set(true);
        return of(undefined);
      },
      () => this._archive() !== null || this._loadouts() !== null,
    );
    setInterval(() => this._now.set(Date.now()), 60_000);

    effect(
      () => {
        if (this.eventBus.logout()) {
          untracked(() => this.reset());
        }
      },
    );

    effect(
      () => {
        const envelope = this.gameEvents.eventEnvelope.CharacterLevelUp();
        const levelUp = envelope?.payload;
        const loadouts = this._loadouts();
        if (
          levelUp &&
          loadouts &&
          levelUp.characterId === this.characterState.currentCharacterId() &&
          levelUp.unlockedEssenceSlots > loadouts.unlockedSlots &&
          this.eventDeduper.shouldProcess('essence-slot-unlock', envelope)
        ) {
          untracked(() => this.markDirty());
        }
      },
    );

    effect(
      () => {
        if (this._activeView() !== 'creatures') {
          this._highlightEssenceFocus.set(false);
          return;
        }

        const readyKey = this.essenceFocusReadyKey();
        if (readyKey && readyKey !== this._seenEssenceFocusReadyKey()) {
          this._highlightEssenceFocus.set(true);
          this._seenEssenceFocusReadyKey.set(readyKey);
        }
      },
    );
  }

  setActiveView(view: EssenceView): void {
    const enteredCreatureArchive =
      view === 'creatures' && this._activeView() !== 'creatures';
    this._activeView.set(view);

    if (enteredCreatureArchive && this._creatureArchive()) {
      this.refreshCreatureArchive();
    }
  }

  refresh(preserveLoadoutDraft = false): void {
    this.synchronize(preserveLoadoutDraft).subscribe({
      error: () => undefined,
    });
  }

  private markDirty(): void {
    this.dirtyVersion += 1;
    this._dirty.set(true);
  }

  private latestRevision(): number {
    return Math.max(
      this.stateSync.latestRevision('essences'),
      this.domainVersions.latest('essences'),
    );
  }

  /** Combat invalidations only dirty the cache; page entry owns fetching it. */
  refreshIfDirty(): void {
    const needsRefresh =
      this._dirty() ||
      this.latestRevision() > this.refreshedRevision ||
      !this._archive() ||
      !this._loadouts() ||
      !this._creatureArchive() ||
      !this._codex();
    if (needsRefresh && !this._loading()) this.refresh(true);
  }

  private synchronize(preserveLoadoutDraft = false): Observable<unknown> {
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;
    const requestDirtyVersion = this.dirtyVersion;
    const requestRevision = this.latestRevision();
    const refreshEpoch = ++this.fullRefreshEpoch;
    const archiveEpoch = ++this.archiveRequestEpoch;
    const loadoutEpoch = ++this.loadoutRequestEpoch;
    const creatureArchiveEpoch = ++this.creatureArchiveRequestEpoch;
    const codexEpoch = ++this.codexRequestEpoch;

    return forkJoin({
      archive: this.essencesService.getArchive(),
      loadouts: this.essencesService.getLoadouts(),
      creatureArchive: this.essencesService.getCreatureArchive(),
      codex: this.essencesService.getCodex(),
    }).pipe(
      tap({
        next: ({ archive, loadouts, creatureArchive, codex }) => {
          if (
            requestVersion !== this.resetVersion ||
            refreshEpoch !== this.fullRefreshEpoch
          ) {
            return;
          }
          const shouldPreserveLoadoutDraft =
            preserveLoadoutDraft &&
            this.hasDraftChanges() &&
            this.canPreserveLoadoutDraft(loadouts);
          if (archiveEpoch === this.archiveRequestEpoch) {
            this._archive.set(archive);
            this.ensureSelectedEssence(archive);
          }
          if (loadoutEpoch === this.loadoutRequestEpoch) {
            this._loadouts.set(loadouts);
            this.ensureSelectedLoadout(loadouts, shouldPreserveLoadoutDraft);
          }
          if (creatureArchiveEpoch === this.creatureArchiveRequestEpoch) {
            this._creatureArchive.set(creatureArchive);
          }
          if (codexEpoch === this.codexRequestEpoch) this._codex.set(codex);
          this.refreshedRevision = requestRevision;
          this._dirty.set(
            requestDirtyVersion !== this.dirtyVersion ||
              this.latestRevision() > requestRevision,
          );
          this.stateSync.activate('essences', 'essences');
        },
        error: (error) => {
          if (
            requestVersion === this.resetVersion &&
            refreshEpoch === this.fullRefreshEpoch
          ) {
            this._dirty.set(true);
            this._error.set(error?.message ?? 'Failed to load essences');
          }
        },
      }),
      finalize(() => {
        if (
          requestVersion === this.resetVersion &&
          refreshEpoch === this.fullRefreshEpoch
        ) {
          this._loading.set(false);
        }
      }),
    );
  }

  /**
   * Refetch the Soul Archive alone. Attunement (PlayerEssenceDto.attunedSlot) is what the
   * Archive list renders, and it changes whenever the default loadout's slots change.
   */
  refreshArchive(): void {
    const requestVersion = this.resetVersion;
    const requestEpoch = ++this.archiveRequestEpoch;

    this.essencesService.getArchive().subscribe({
      next: (archive) => {
        if (
          requestVersion !== this.resetVersion ||
          requestEpoch !== this.archiveRequestEpoch
        ) {
          return;
        }
        this._archive.set(archive);
        this.ensureSelectedEssence(archive);
      },
      error: (error) => {
        if (
          requestVersion !== this.resetVersion ||
          requestEpoch !== this.archiveRequestEpoch
        ) {
          return;
        }
        this._error.set(error?.message ?? 'Failed to load Soul Archive');
      },
    });
  }

  refreshCreatureArchive(): void {
    const requestVersion = this.resetVersion;
    const requestEpoch = ++this.creatureArchiveRequestEpoch;

    this.essencesService.getCreatureArchive().subscribe({
      next: (creatureArchive) => {
        if (
          requestVersion !== this.resetVersion ||
          requestEpoch !== this.creatureArchiveRequestEpoch
        ) {
          return;
        }
        this._creatureArchive.set(creatureArchive);
      },
      error: (error) => {
        if (
          requestVersion !== this.resetVersion ||
          requestEpoch !== this.creatureArchiveRequestEpoch
        ) {
          return;
        }
        this._error.set(error?.message ?? 'Failed to load Creature Archive');
      },
    });
  }

  reset(): void {
    this.dirtyVersion = 0;
    this.refreshedRevision = 0;
    this._dirty.set(true);
    this.resetVersion += 1;
    this.dustMutationVersion += 1;
    this.fullRefreshEpoch += 1;
    this.archiveRequestEpoch += 1;
    this.loadoutRequestEpoch += 1;
    this.creatureArchiveRequestEpoch += 1;
    this.codexRequestEpoch += 1;
    this._activeView.set('archive');
    this._archive.set(null);
    this._loadouts.set(null);
    this._creatureArchive.set(null);
    this._codex.set(null);
    this._selectedPlayerEssenceId.set(null);
    this._selectedLoadoutId.set(null);
    this._selectedInventoryItemId.set(null);
    this._draftLoadoutName.set('Default');
    this._draftSlots.set([]);
    this._savingLoadout.set(false);
    this._loading.set(false);
    this._spendingDust.set(false);
    this._error.set(null);
    this._seenEssenceFocusReadyKey.set(null);
    this._highlightEssenceFocus.set(false);
  }

  selectPlayerEssence(essence: PlayerEssenceDto): void {
    this._selectedPlayerEssenceId.set(essence.id);
  }

  selectInventoryEssence(inventoryItem: InventoryItem): void {
    this._selectedInventoryItemId.set(inventoryItem.itemInstance.id);
  }

  spendDust(essence: PlayerEssenceDto): void {
    if (this._spendingDust()) return;

    const resetVersion = this.resetVersion;
    const mutationVersion = ++this.dustMutationVersion;
    this._spendingDust.set(true);
    this._error.set(null);

    this.essencesService
      .spendDust(essence.id, 1)
      .pipe(
        finalize(() => {
          if (mutationVersion === this.dustMutationVersion) {
            this._spendingDust.set(false);
          }
        }),
      )
      .subscribe({
        next: (result) => {
          if (
            resetVersion !== this.resetVersion ||
            mutationVersion !== this.dustMutationVersion
          ) {
            return;
          }

          if (!result.data.succeeded) {
            this._error.set(
              result.data.message || 'Failed to level up essence',
            );
            return;
          }

          this.applyEssenceMutation(result);
        },
        error: (error) => {
          if (
            resetVersion === this.resetVersion &&
            mutationVersion === this.dustMutationVersion
          ) {
            const message = this.getRequestErrorMessage(
              error,
              'Failed to level up essence',
            );

            if (this.isBadRequest(error)) {
              this.reconcileArchiveAfterDustRejection(message, resetVersion);
              return;
            }

            this._error.set(message);
          }
        },
      });
  }

  ascend(essence: PlayerEssenceDto): void {
    this.essencesService
      .ascend(essence.id)
      .subscribe((response) => this.applyEssenceMutation(response));
  }

  evolve(essence: PlayerEssenceDto): void {
    this.essencesService
      .evolve(essence.id)
      .subscribe((response) => this.applyEssenceMutation(response));
  }

  favorite(essence: PlayerEssenceDto): void {
    const isFavorite = !essence.isFavorite;
    const mutationEpoch = ++this.archiveRequestEpoch;
    this.updateFavorite(essence.id, isFavorite);
    this.essencesService.setFavorite(essence.id, isFavorite).subscribe({
      next: (result) => this.applyEssenceState(result),
      error: (error) => {
        if (mutationEpoch !== this.archiveRequestEpoch) return;
        this.updateFavorite(essence.id, !isFavorite);
        this._error.set(error?.message ?? 'Failed to update favorite');
      },
    });
  }

  setEssenceFocus(creatureId: string | null): void {
    if (creatureId && !this.canChangeEssenceFocus()) {
      this._error.set('Essence Focus can be changed once every 8 hours.');
      return;
    }

    this.essencesService.setEssenceFocus(creatureId).subscribe({
      next: (result) => this.applyEssenceState(result),
      error: (error) =>
        this._error.set(error?.message ?? 'Failed to update Essence Focus'),
    });
  }

  absorbSelectedInventoryEssence(): Observable<EssenceMutationResponseDto> | null {
    const inventoryItemId = this._selectedInventoryItemId();
    const item = this.selectedInventoryItem();
    if (!item || !inventoryItemId || this.isInventoryEssenceAbsorbed(item)) {
      return null;
    }

    this._error.set(null);
    return this.essencesService.absorb(inventoryItemId).pipe(
      tap((result) => {
        const response = result.data;
        if (!response.succeeded) {
          this._error.set(response.message || 'Failed to absorb essence');
          return;
        }

        this.applyEssenceMutation(result);
        this._selectedInventoryItemId.set(
          this.getFirstAbsorbableInventoryEssenceId(),
        );
      }),
      map((result) => result.data),
      catchError((error) => {
        this._error.set(error?.message ?? 'Failed to absorb essence');
        return EMPTY;
      }),
    );
  }

  dismantleSelectedInventoryEssence(
    quantity = 1,
  ): Observable<EssenceMutationResponseDto> | null {
    const item = this.selectedInventoryItem();
    if (!item) return null;

    return this.dismantleInventoryEssences([
      { inventoryItemId: item.itemInstance.id, quantity },
    ]);
  }

  dismantleInventoryEssences(
    selections: readonly { inventoryItemId: string; quantity: number }[],
  ): Observable<EssenceMutationResponseDto> | null {
    const validSelections = selections.filter(
      (selection) => selection.quantity > 0,
    );
    if (validSelections.length === 0) return null;

    this._error.set(null);
    return from(validSelections).pipe(
      concatMap((selection) =>
        this.essencesService
          .dismantle(selection.inventoryItemId, selection.quantity)
          .pipe(
            map((result) => {
              const response = result.data;
              if (!response.succeeded) {
                this._error.set(
                  response.message || 'Failed to shatter essence',
                );
                return response;
              }

              this.applyEssenceMutation(result);
              return response;
            }),
          ),
      ),
      takeWhile((response) => response.succeeded, true),
      last(),
      tap((response) => {
        if (response.succeeded) this._selectedInventoryItemId.set(null);
      }),
      catchError((error) => {
        this._error.set(error?.message ?? 'Failed to shatter essence');
        return EMPTY;
      }),
    );
  }

  newLoadout(): void {
    this._selectedLoadoutId.set(null);
    this._draftLoadoutName.set(NEW_LOADOUT_NAME);
    this._draftSlots.set(this.slotIndexes().map(() => null));
  }

  selectLoadout(loadout: EssenceLoadoutDto): void {
    this._selectedLoadoutId.set(loadout.id);
    this._draftLoadoutName.set(loadout.name);
    this._draftSlots.set(this.getLoadoutDraftSlots(loadout));
  }

  setDraftLoadoutName(name: string): void {
    this._draftLoadoutName.set(name);
  }

  setDraftSlot(slotIndex: number, playerEssenceId: string | null): void {
    const slots = [...this._draftSlots()];
    const nextPlayerEssenceId = playerEssenceId || null;

    if (nextPlayerEssenceId) {
      const nextCreatureId =
        this.getCreatureIdForPlayerEssence(nextPlayerEssenceId);
      slots.forEach((slotPlayerEssenceId, index) => {
        if (
          index !== slotIndex &&
          (slotPlayerEssenceId === nextPlayerEssenceId ||
            (!!nextCreatureId &&
              !!slotPlayerEssenceId &&
              this.getCreatureIdForPlayerEssence(slotPlayerEssenceId) ===
                nextCreatureId))
        ) {
          slots[index] = null;
        }
      });
    }

    slots[slotIndex] = nextPlayerEssenceId;
    this._draftSlots.set(slots);
  }

  canAssignEssenceToDraftSlot(
    slotIndex: number,
    playerEssenceId: string,
  ): boolean {
    if (this._draftSlots()[slotIndex] === playerEssenceId) return true;

    const creatureId = this.getCreatureIdForPlayerEssence(playerEssenceId);
    return this._draftSlots().every(
      (assignedId, index) =>
        index === slotIndex ||
        !assignedId ||
        (assignedId !== playerEssenceId &&
          (!creatureId ||
            this.getCreatureIdForPlayerEssence(assignedId) !== creatureId)),
    );
  }

  saveDraftLoadout(): void {
    if (!this.canSaveDraft()) return;

    this.persistDraftLoadout(false);
  }

  saveDraftSlots(): void {
    if (!this.canPersistDraftLoadout()) return;

    this.persistDraftLoadout(true);
  }

  private persistDraftLoadout(restoreSavedDraftOnError: boolean): void {
    const selectedLoadout = this.selectedLoadout();
    const pendingDraftName = this._draftLoadoutName();
    const preservePendingName =
      restoreSavedDraftOnError &&
      !!selectedLoadout &&
      this.hasDraftNameChanges();
    const name = preservePendingName
      ? selectedLoadout.name
      : pendingDraftName.trim();
    this._savingLoadout.set(true);
    this._error.set(null);

    const slots: SaveEssenceLoadoutSlotDto[] = this._draftSlots()
      .map((playerEssenceId, slotIndex) => ({ slotIndex, playerEssenceId }))
      .filter((slot) => !!slot.playerEssenceId);

    const id = this._selectedLoadoutId();
    const request = { id, name, slots };
    const save = id
      ? this.essencesService.updateLoadout(id, request)
      : this.essencesService.saveLoadout(request);

    save.subscribe({
      next: (result) => {
        const loadout = result.data.savedLoadout;
        if (!loadout || !this.applyEssenceState(result, loadout.id)) {
          this._savingLoadout.set(false);
          return;
        }
        if (preservePendingName) {
          this._draftLoadoutName.set(pendingDraftName);
        }

        this._savingLoadout.set(false);
        this.characterState.markOverviewDirty();
      },
      error: (error) => {
        this._savingLoadout.set(false);
        if (restoreSavedDraftOnError) {
          const selectedLoadout = this.selectedLoadout();
          selectedLoadout
            ? this.selectLoadout(selectedLoadout)
            : this.newLoadout();
          if (preservePendingName) {
            this._draftLoadoutName.set(pendingDraftName);
          }
        }
        this._error.set(error?.message ?? 'Failed to save Essence loadout');
      },
    });
  }

  private canPersistDraftLoadout(): boolean {
    const loadouts = this._loadouts();
    const selectedId = this._selectedLoadoutId();
    if (!loadouts || this._savingLoadout()) return false;
    const persistedName =
      this.selectedLoadout()?.name ?? this._draftLoadoutName().trim();

    return (
      !!persistedName &&
      this.hasDraftChanges() &&
      !this.hasDuplicateDraftEssences() &&
      !this.hasDuplicateDraftCreatureSources() &&
      (!!selectedId || loadouts.loadouts.length < loadouts.limit)
    );
  }

  setSelectedLoadoutAutoUseActivities(
    activities: readonly EssenceCombatActivity[],
  ): void {
    const id = this._selectedLoadoutId();
    if (!id || this._savingLoadout()) return;

    const preserveDraft = this.hasDraftChanges();
    const draftName = this._draftLoadoutName();
    const draftSlots = [...this._draftSlots()];
    this._savingLoadout.set(true);
    this._error.set(null);
    this.essencesService.setLoadoutAutoUseActivities(id, activities).subscribe({
      next: (result) => {
        if (this.applyEssenceState(result, id) && preserveDraft) {
          this._draftLoadoutName.set(draftName);
          this._draftSlots.set(draftSlots);
        }
        this._savingLoadout.set(false);
      },
      error: (error) => {
        this._savingLoadout.set(false);
        this._error.set(
          error?.message ?? 'Failed to update automatic loadout use',
        );
      },
    });
  }

  deleteSelectedLoadout(): void {
    const id = this._selectedLoadoutId();
    if (!id) return;
    this.essencesService.deleteLoadout(id).subscribe((result) => {
      if (this.applyEssenceState(result)) {
        this.characterState.markOverviewDirty();
      }
    });
  }

  asEssence(inventoryItem: InventoryItem): Essence {
    const item = inventoryItem.itemInstance.itemBase as EssenceItem;
    return this.essenceItemView.asEssence(item);
  }

  isInventoryEssenceAbsorbed(inventoryItem: InventoryItem): boolean {
    return this.absorbedEssenceDefinitionIds().has(
      this.getEssenceDefinitionId(inventoryItem),
    );
  }

  selectedInventoryEssenceQuantity(): number {
    const selected = this.selectedInventoryEssence();
    if (!selected) return 1;

    return (
      this.inventoryEssences().find(
        (item) => this.asEssence(item).id === selected.id,
      )?.quantity ?? 1
    );
  }

  private ensureSelectedEssence(archive: SoulArchiveDto): void {
    const selectedId = this._selectedPlayerEssenceId();
    if (
      selectedId &&
      archive.essences.some((essence) => essence.id === selectedId)
    ) {
      return;
    }

    this._selectedPlayerEssenceId.set(archive.essences[0]?.id ?? null);
  }

  private applyEssenceMutation(
    result: VersionedMutationResult<EssenceMutationResponseDto>,
  ): boolean {
    const response = result.data;
    const appliesEssences = this.applyEssenceState(result);

    this.inventoryState.applyVersionedInventory(result);
    if (
      this.domainVersions.isCurrent(
        'equipment',
        result.domainVersions['equipment'],
      )
    ) {
      this.equipmentState.setSlots(response.equipmentSlots);
    }
    this.characterState.markOverviewDirty();
    return appliesEssences;
  }

  private applyEssenceState(
    result: VersionedMutationResult<EssenceStateResponseDto>,
    preferredLoadoutId?: string,
  ): boolean {
    if (
      !this.domainVersions.isCurrent(
        'essences',
        result.domainVersions['essences'],
      )
    ) {
      return false;
    }

    const response = result.data;
    this.fullRefreshEpoch += 1;
    this.archiveRequestEpoch += 1;
    this.loadoutRequestEpoch += 1;
    this.creatureArchiveRequestEpoch += 1;
    this.codexRequestEpoch += 1;
    this._archive.set(response.archive);
    this._loadouts.set(response.loadouts);
    this._creatureArchive.set(response.creatureArchive);
    this._codex.set(response.codex);
    this.refreshedRevision =
      result.domainVersions['essences'] ?? this.latestRevision();
    this._dirty.set(false);
    this._loading.set(false);
    this.ensureSelectedEssence(response.archive);

    const preferredLoadout = preferredLoadoutId
      ? response.loadouts.loadouts.find(
          (loadout) => loadout.id === preferredLoadoutId,
        )
      : null;
    preferredLoadout
      ? this.selectLoadout(preferredLoadout)
      : this.ensureSelectedLoadout(response.loadouts);
    return true;
  }

  private updateFavorite(essenceId: string, isFavorite: boolean): void {
    this._archive.update((archive) =>
      archive
        ? {
            ...archive,
            essences: archive.essences.map((entry) =>
              entry.id === essenceId ? { ...entry, isFavorite } : entry,
            ),
          }
        : archive,
    );
  }

  private reconcileArchiveAfterDustRejection(
    message: string,
    resetVersion: number,
  ): void {
    const reconciliationVersion = ++this.dustMutationVersion;
    const requestEpoch = ++this.archiveRequestEpoch;

    this.essencesService
      .getArchive()
      .pipe(
        finalize(() => {
          if (
            resetVersion === this.resetVersion &&
            reconciliationVersion === this.dustMutationVersion &&
            requestEpoch === this.archiveRequestEpoch
          ) {
            this._spendingDust.set(false);
          }
        }),
      )
      .subscribe({
        next: (archive) => {
          if (
            resetVersion !== this.resetVersion ||
            reconciliationVersion !== this.dustMutationVersion ||
            requestEpoch !== this.archiveRequestEpoch
          ) {
            return;
          }

          this._archive.set(archive);
          this.ensureSelectedEssence(archive);
          this._error.set(message);
        },
        error: () => {
          if (
            resetVersion === this.resetVersion &&
            reconciliationVersion === this.dustMutationVersion &&
            requestEpoch === this.archiveRequestEpoch
          ) {
            this._error.set(message);
          }
        },
      });
  }

  private isBadRequest(error: unknown): boolean {
    return (
      typeof error === 'object' &&
      error !== null &&
      'status' in error &&
      error.status === 400
    );
  }

  private getRequestErrorMessage(error: unknown, fallback: string): string {
    if (typeof error !== 'object' || error === null) return fallback;

    const requestError = error as {
      errorMessage?: unknown;
      message?: unknown;
    };
    if (
      typeof requestError.errorMessage === 'string' &&
      requestError.errorMessage.trim()
    ) {
      return requestError.errorMessage;
    }
    if (
      typeof requestError.message === 'string' &&
      requestError.message.trim()
    ) {
      return requestError.message;
    }

    return fallback;
  }

  private ensureSelectedLoadout(
    loadouts: EssenceLoadoutsDto,
    preserveDraft = false,
  ): void {
    if (preserveDraft) return;

    const selectedLoadout =
      loadouts.loadouts.find(
        (loadout) => loadout.id === this._selectedLoadoutId(),
      ) ??
      loadouts.loadouts[0] ??
      null;

    selectedLoadout ? this.selectLoadout(selectedLoadout) : this.newLoadout();
  }

  private canPreserveLoadoutDraft(loadouts: EssenceLoadoutsDto): boolean {
    const selectedLoadoutId = this._selectedLoadoutId();
    return (
      selectedLoadoutId === null ||
      loadouts.loadouts.some((loadout) => loadout.id === selectedLoadoutId)
    );
  }

  private getLoadoutDraftSlots(loadout: EssenceLoadoutDto): (string | null)[] {
    return this.slotIndexes().map((slotIndex) => {
      const slot = loadout.slots.find(
        (loadoutSlot) => loadoutSlot.slotIndex === slotIndex,
      );
      return slot?.playerEssenceId ?? null;
    });
  }

  private getCreatureIdForPlayerEssence(
    playerEssenceId: string,
  ): string | null {
    const definitionId = this._archive()?.essences.find(
      (essence) => essence.id === playerEssenceId,
    )?.essenceDefinitionId;
    if (!definitionId) return null;

    return (
      this._creatureArchive()?.creatures.find((creature) =>
        creature.essences.some(
          (essence) => essence.essenceDefinitionId === definitionId,
        ),
      )?.creatureId ?? null
    );
  }

  private getEssenceDefinitionId(inventoryItem: InventoryItem): string {
    return inferEssenceDefinitionId(
      inventoryItem.itemInstance.itemBase as EssenceItem,
    );
  }

  private getFirstAbsorbableInventoryEssenceId(): string | null {
    return (
      this.inventoryEssences().find(
        (item) => !this.isInventoryEssenceAbsorbed(item),
      )?.itemInstance.id ?? null
    );
  }

  private getUtcTime(value: string | null | undefined): number | null {
    if (!value) return null;

    const time = new Date(value).getTime();
    return Number.isNaN(time) ? null : time;
  }
}
