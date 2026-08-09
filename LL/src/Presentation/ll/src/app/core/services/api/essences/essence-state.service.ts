import { Injectable, computed, effect, signal } from '@angular/core';
import { EMPTY, forkJoin, Observable, catchError, finalize, tap } from 'rxjs';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { QuestStateService } from '../quest/quest-state.service';
import { EssencesService } from './essences.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CharacterStateService } from '../character/character-state.service';
import { Essence } from '../../../../shared/models/essence';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import {
  EssenceItem,
  inferEssenceDefinitionId,
} from '../../../../shared/models/item';
import {
  CreatureArchiveDto,
  EssenceCodexDto,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  EssenceMutationResponseDto,
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
  private readonly _loading = signal(false);
  private readonly _spendingDust = signal(false);
  private readonly _error = signal<string | null>(null);
  private resetVersion = 0;
  private dustMutationVersion = 0;

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
  readonly loading = computed(() => this._loading());
  readonly spendingDust = computed(() => this._spendingDust());
  readonly error = computed(() => this._error());

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

  readonly canSaveDraft = computed(() => {
    const name = this._draftLoadoutName().trim();
    const loadouts = this._loadouts();
    const selectedId = this._selectedLoadoutId();
    if (!loadouts) return false;

    const canCreate = loadouts.loadouts.length < loadouts.limit;

    return (
      !!name &&
      this.hasDraftChanges() &&
      !this.hasDuplicateDraftEssences() &&
      !this.hasDuplicateDraftCreatureSources() &&
      (!!selectedId || canCreate)
    );
  });

  constructor(
    private readonly essencesService: EssencesService,
    private readonly inventoryState: InventoryStateService,
    private readonly essenceItemView: EssenceItemViewService,
    private readonly questState: QuestStateService,
    private readonly eventBus: EventBusService,
    private readonly characterState: CharacterStateService,
  ) {
    setInterval(() => this._now.set(Date.now()), 60_000);

    effect(
      () => {
        if (this.eventBus.logout()) {
          this.reset();
        }
      },
      { allowSignalWrites: true },
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
      { allowSignalWrites: true },
    );
  }

  setActiveView(view: EssenceView): void {
    this._activeView.set(view);
  }

  refresh(): void {
    this._loading.set(true);
    this._error.set(null);
    const requestVersion = this.resetVersion;

    forkJoin({
      archive: this.essencesService.getArchive(),
      loadouts: this.essencesService.getLoadouts(),
      creatureArchive: this.essencesService.getCreatureArchive(),
      codex: this.essencesService.getCodex(),
    }).subscribe({
      next: ({ archive, loadouts, creatureArchive, codex }) => {
        if (requestVersion !== this.resetVersion) return;
        this._archive.set(archive);
        this._loadouts.set(loadouts);
        this._creatureArchive.set(creatureArchive);
        this._codex.set(codex);
        this.ensureSelectedEssence(archive);
        this.ensureSelectedLoadout(loadouts);
        this._loading.set(false);
      },
      error: (error) => {
        if (requestVersion !== this.resetVersion) return;
        this._error.set(error?.message ?? 'Failed to load essences');
        this._loading.set(false);
      },
    });
  }

  refreshCreatureArchive(): void {
    const requestVersion = this.resetVersion;

    this.essencesService.getCreatureArchive().subscribe({
      next: (creatureArchive) => {
        if (requestVersion !== this.resetVersion) return;
        this._creatureArchive.set(creatureArchive);
      },
      error: (error) => {
        if (requestVersion !== this.resetVersion) return;
        this._error.set(error?.message ?? 'Failed to load Creature Archive');
      },
    });
  }

  reset(): void {
    this.resetVersion += 1;
    this.dustMutationVersion += 1;
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
        next: (response) => {
          if (
            resetVersion !== this.resetVersion ||
            mutationVersion !== this.dustMutationVersion
          ) {
            return;
          }

          if (!response.succeeded) {
            this._error.set(response.message || 'Failed to level up essence');
            return;
          }

          this.applyEssenceMutation(response);
        },
        error: (error) => {
          if (
            resetVersion === this.resetVersion &&
            mutationVersion === this.dustMutationVersion
          ) {
            this._error.set(error?.message ?? 'Failed to level up essence');
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
    essence.isFavorite = !essence.isFavorite;
    this.essencesService
      .setFavorite(essence.id, essence.isFavorite)
      .subscribe();
  }

  setEssenceFocus(creatureId: string | null): void {
    if (creatureId && !this.canChangeEssenceFocus()) {
      this._error.set('Essence Focus can be changed once every 8 hours.');
      return;
    }

    this.essencesService.setEssenceFocus(creatureId).subscribe({
      next: (archive) => this._creatureArchive.set(archive),
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
      tap((response) => {
        if (!response.succeeded) {
          this._error.set(response.message || 'Failed to absorb essence');
          return;
        }

        this.applyEssenceMutation(response);
        this.refreshLoadouts();
        this.questState.refreshAfterOutboxProgress();
        this._selectedInventoryItemId.set(
          this.getFirstAbsorbableInventoryEssenceId(),
        );
      }),
      catchError((error) => {
        this._error.set(error?.message ?? 'Failed to absorb essence');
        return EMPTY;
      }),
    );
  }

  dismantleSelectedInventoryEssence(): Observable<EssenceMutationResponseDto> | null {
    const item = this.selectedInventoryItem();
    if (!item) return null;

    this._error.set(null);
    return this.essencesService.dismantle(item.itemInstance.id).pipe(
      tap((response) => {
        if (!response.succeeded) {
          this._error.set(response.message || 'Failed to shatter essence');
          return;
        }

        this.applyEssenceMutation(response);
        this._selectedInventoryItemId.set(null);
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

  saveDraftLoadout(activateAfterSave = false): void {
    const name = this._draftLoadoutName().trim();
    if (!this.canSaveDraft()) return;

    const slots: SaveEssenceLoadoutSlotDto[] = this._draftSlots()
      .map((playerEssenceId, slotIndex) => ({ slotIndex, playerEssenceId }))
      .filter((slot) => !!slot.playerEssenceId);

    const id = this._selectedLoadoutId();
    const request = { id, name, slots };
    const save = id
      ? this.essencesService.updateLoadout(id, request)
      : this.essencesService.saveLoadout(request);

    save.subscribe({
      next: (loadout) => {
        this._selectedLoadoutId.set(loadout.id);

        if (activateAfterSave && !loadout.isActive) {
          this.essencesService.activateLoadout(loadout.id).subscribe({
            next: () => {
              this.characterState.markOverviewDirty();
              this.refresh();
              this.questState.refreshAfterOutboxProgress();
            },
            error: (error) =>
              this._error.set(
                error?.message ?? 'Failed to activate Essence loadout',
              ),
          });
          return;
        }

        if (loadout.isActive) {
          this.characterState.markOverviewDirty();
        }
        this.refresh();
        if (activateAfterSave) {
          this.questState.refreshAfterOutboxProgress();
        }
      },
      error: (error) =>
        this._error.set(error?.message ?? 'Failed to save Essence loadout'),
    });
  }

  activateSelectedLoadout(): void {
    const id = this._selectedLoadoutId();
    if (!id) return;
    this.essencesService.activateLoadout(id).subscribe(() => {
      this.characterState.markOverviewDirty();
      this.refresh();
      this.questState.refreshAfterOutboxProgress();
    });
  }

  deleteSelectedLoadout(): void {
    const id = this._selectedLoadoutId();
    if (!id) return;
    const deletesActiveLoadout = this.selectedLoadout()?.isActive === true;
    this.essencesService.deleteLoadout(id).subscribe(() => {
      this._selectedLoadoutId.set(null);
      if (deletesActiveLoadout) {
        this.characterState.markOverviewDirty();
      }
      this.refresh();
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

  private applyEssenceMutation(response: EssenceMutationResponseDto): void {
    this._archive.set(response.archive);
    this.inventoryState.setInventory(response.inventoryItems);
    this.characterState.markOverviewDirty();
    this.ensureSelectedEssence(response.archive);
    this.refreshCompanionArchives();
  }

  private refreshCompanionArchives(): void {
    const requestVersion = this.resetVersion;

    forkJoin({
      creatureArchive: this.essencesService.getCreatureArchive(),
      codex: this.essencesService.getCodex(),
    }).subscribe({
      next: ({ creatureArchive, codex }) => {
        if (requestVersion !== this.resetVersion) return;
        this._creatureArchive.set(creatureArchive);
        this._codex.set(codex);
      },
      error: (error) => {
        if (requestVersion !== this.resetVersion) return;
        this._error.set(error?.message ?? 'Failed to refresh Essence records');
      },
    });
  }

  private refreshLoadouts(): void {
    this._loading.set(true);
    const requestVersion = this.resetVersion;

    this.essencesService
      .getLoadouts()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (loadouts) => {
          if (requestVersion !== this.resetVersion) return;
          this._loadouts.set(loadouts);
          this.ensureSelectedLoadout(loadouts);
        },
        error: (error) => {
          if (requestVersion !== this.resetVersion) return;
          this._error.set(error?.message ?? 'Failed to load Essence loadouts');
        },
      });
  }

  private ensureSelectedLoadout(loadouts: EssenceLoadoutsDto): void {
    const selectedLoadout =
      loadouts.loadouts.find(
        (loadout) => loadout.id === this._selectedLoadoutId(),
      ) ??
      loadouts.loadouts.find((loadout) => loadout.isActive) ??
      loadouts.loadouts[0] ??
      null;

    selectedLoadout ? this.selectLoadout(selectedLoadout) : this.newLoadout();
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
