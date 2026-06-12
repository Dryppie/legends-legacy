import { Injectable, computed, signal } from '@angular/core';
import { forkJoin, Observable, tap } from 'rxjs';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { EssencesService } from './essences.service';
import { EssenceCatalogViewService } from './essence-catalog-view.service';
import { Essence } from '../../../../shared/models/essence';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import { EssenceItem } from '../../../../shared/models/item';
import {
  DismantleEssenceResultDto,
  EssenceCatalogDto,
  EssenceLoadoutDto,
  EssenceLoadoutsDto,
  PlayerEssenceDto,
  ResponseMessageDto,
  SaveEssenceLoadoutSlotDto,
  SoulArchiveDto,
} from '../../../../shared/models/essence-system';

type EssenceView = 'archive' | 'absorb';

@Injectable({ providedIn: 'root' })
export class EssenceStateService {
  private readonly _activeView = signal<EssenceView>('archive');
  private readonly _catalog = signal<EssenceCatalogDto | null>(null);
  private readonly _archive = signal<SoulArchiveDto | null>(null);
  private readonly _loadouts = signal<EssenceLoadoutsDto | null>(null);
  private readonly _selectedPlayerEssenceId = signal<string | null>(null);
  private readonly _selectedLoadoutId = signal<string | null>(null);
  private readonly _selectedInventoryItemId = signal<string | null>(null);
  private readonly _draftLoadoutName = signal('Default');
  private readonly _draftSlots = signal<(string | null)[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly activeView = computed(() => this._activeView());
  readonly catalog = computed(() => this._catalog());
  readonly archive = computed(() => this._archive());
  readonly loadouts = computed(() => this._loadouts());
  readonly selectedLoadoutId = computed(() => this._selectedLoadoutId());
  readonly draftLoadoutName = computed(() => this._draftLoadoutName());
  readonly draftSlots = computed(() => this._draftSlots());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());

  readonly inventoryEssences = computed(() =>
    this.inventoryState
      .items()
      .filter((item) => item.itemInstance.itemBase.itemType === ItemType.Essence),
  );

  readonly absorbedEssenceDefinitionIds = computed(
    () =>
      new Set(
        this._archive()?.essences.map((essence) => essence.essenceDefinitionId) ??
          [],
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

  readonly canSaveDraft = computed(() => {
    const name = this._draftLoadoutName().trim();
    const loadouts = this._loadouts();
    const selectedId = this._selectedLoadoutId();
    const canCreate = !loadouts || loadouts.loadouts.length < loadouts.limit;

    return !!name && !this.hasDuplicateDraftEssences() && (!!selectedId || canCreate);
  });

  constructor(
    private readonly essencesService: EssencesService,
    private readonly inventoryState: InventoryStateService,
    private readonly essenceCatalogView: EssenceCatalogViewService,
  ) {}

  setActiveView(view: EssenceView): void {
    this._activeView.set(view);
  }

  refresh(): void {
    this._loading.set(true);
    this._error.set(null);

    forkJoin({
      catalog: this.essencesService.getCatalog(),
      archive: this.essencesService.getArchive(),
      loadouts: this.essencesService.getLoadouts(),
    }).subscribe({
      next: ({ catalog, archive, loadouts }) => {
        this._catalog.set(catalog);
        this.essenceCatalogView.setCatalog(catalog);
        this._archive.set(archive);
        this._loadouts.set(loadouts);
        this.ensureSelectedEssence(archive);
        this.ensureSelectedLoadout(loadouts);
        this._loading.set(false);
      },
      error: (error) => {
        this._error.set(error?.message ?? 'Failed to load essences');
        this._loading.set(false);
      },
    });
  }

  selectPlayerEssence(essence: PlayerEssenceDto): void {
    this._selectedPlayerEssenceId.set(essence.id);
  }

  selectInventoryEssence(inventoryItem: InventoryItem): void {
    this._selectedInventoryItemId.set(inventoryItem.itemInstance.id);
  }

  spendDust(essence: PlayerEssenceDto): void {
    this.essencesService.spendDust(essence.id, 1).subscribe(() => this.refresh());
  }

  ascend(essence: PlayerEssenceDto): void {
    this.essencesService.ascend(essence.id).subscribe(() => this.refresh());
  }

  evolve(essence: PlayerEssenceDto): void {
    this.essencesService.evolve(essence.id).subscribe(() => this.refresh());
  }

  favorite(essence: PlayerEssenceDto): void {
    this.essencesService
      .setFavorite(essence.id, !essence.isFavorite)
      .subscribe(() => this.refresh());
  }

  absorbSelectedInventoryEssence(): Observable<ResponseMessageDto> | null {
    const inventoryItemId = this._selectedInventoryItemId();
    const item = this.selectedInventoryItem();
    if (!item || !inventoryItemId || this.isInventoryEssenceAbsorbed(item)) {
      return null;
    }

    return this.essencesService.absorb(inventoryItemId).pipe(
      tap((response) => {
        if (!response.succeeded) return;

        this.inventoryState.decrementItem(inventoryItemId, 1);
        this._selectedInventoryItemId.set(null);
        this.refresh();
      }),
    );
  }

  dismantleSelectedInventoryEssence(): Observable<DismantleEssenceResultDto> | null {
    const item = this.selectedInventoryItem();
    if (!item) return null;

    return this.essencesService.dismantle(item.itemInstance.id).pipe(
      tap((response) => {
        if (!response.succeeded) return;

        this.inventoryState.decrementItem(item.itemInstance.id, 1);
        this._selectedInventoryItemId.set(null);
        this.refresh();
      }),
    );
  }

  newLoadout(): void {
    this._selectedLoadoutId.set(null);
    this._draftLoadoutName.set('New Loadout');
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
    slots[slotIndex] = playerEssenceId || null;
    this._draftSlots.set(slots);
  }

  saveDraftLoadout(): void {
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

    save.subscribe((loadout) => {
      this._selectedLoadoutId.set(loadout.id);
      this.refresh();
    });
  }

  activateSelectedLoadout(): void {
    const id = this._selectedLoadoutId();
    if (!id) return;
    this.essencesService.activateLoadout(id).subscribe(() => this.refresh());
  }

  deleteSelectedLoadout(): void {
    const id = this._selectedLoadoutId();
    if (!id) return;
    this.essencesService.deleteLoadout(id).subscribe(() => {
      this._selectedLoadoutId.set(null);
      this.refresh();
    });
  }

  asEssence(inventoryItem: InventoryItem): Essence {
    const item = inventoryItem.itemInstance.itemBase as EssenceItem;
    return this.essenceCatalogView.asEssence(item);
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
    if (selectedId && archive.essences.some((essence) => essence.id === selectedId)) {
      return;
    }

    this._selectedPlayerEssenceId.set(archive.essences[0]?.id ?? null);
  }

  private ensureSelectedLoadout(loadouts: EssenceLoadoutsDto): void {
    const selectedLoadout =
      loadouts.loadouts.find((loadout) => loadout.id === this._selectedLoadoutId()) ??
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

  private getEssenceDefinitionId(inventoryItem: InventoryItem): string {
    return (inventoryItem.itemInstance.itemBase as EssenceItem).essenceDefinitionId;
  }
}
