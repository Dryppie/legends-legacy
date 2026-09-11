import { Injectable, signal, computed, effect, untracked } from '@angular/core';
import { NobilityService } from '../nobility/nobility.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EMPTY,
  finalize,
  forkJoin,
  map,
  Observable,
  switchMap,
  tap,
} from 'rxjs';
import { ApiService } from '../api.service';
import { EquipmentStateService } from './equipment-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterStateService } from '../character/character-state.service';
import { EssenceCombatActivity } from '../../../../shared/models/essence-system';
import { EquipmentInstance } from '../../../../shared/models/item';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';

export interface EquipmentLoadout {
  presetSlot?: number;
  isUsable?: boolean;
  id: string;
  name: string;
  autoUseActivities: EssenceCombatActivity[];
  slots: {
    slotType: EquipmentSlotType;
    equipmentInstanceId: string | null;
    equipmentInstance: EquipmentInstance | null;
  }[];
}

@Injectable({ providedIn: 'root' })
export class EquipmentLoadoutService {
  readonly loadouts = signal<EquipmentLoadout[]>([]);
  readonly usableCount = computed(() => this.loadouts().filter(x => x.isUsable !== false).length);
  readonly selectedId = signal<string | null>(null);
  readonly selected = computed(() =>
    this.loadouts().find((loadout) => loadout.id === this.selectedId()),
  );
  readonly pending = signal(false);
  readonly busy = computed(() => this.pending() || this.equipment.loading());
  readonly unsavedChanges = signal(false);
  readonly error = signal<string | null>(null);
  readonly limit = computed(() => this.nobility.equipmentLimit());
  private epoch = 0;
  private selectionInitialized = false;

  constructor(
    private api: ApiService,
    private equipment: EquipmentStateService,
    private inventory: InventoryStateService,
    private character: CharacterStateService,
    sync: StateSyncCoordinator,
    private events: EventBusService,
    private nobility: NobilityService,
  ) {
    sync.register('equipment', 'equipment-loadouts', () => this.refresh());
    effect(() => {
      if (events.logout())
        untracked(() => {
          this.epoch++;
          this.selectionInitialized = false;
          this.loadouts.set([]);
          this.selectedId.set(null);
          this.unsavedChanges.set(false);
          this.error.set(null);
          this.pending.set(false);
          this.equipment.loadoutPending.set(false);
        });
    });

    // Only successful player equip/unequip actions save a loadout. Refreshes and
    // switching sets must never copy equipment into the previously selected set.
    this.equipment.equipmentChanges$
      .pipe(takeUntilDestroyed())
      .subscribe(() => {
        const loadout = this.selected();
        if (!loadout || loadout.isUsable === false) return;
        this.unsavedChanges.set(true);
        this.saveCurrent(loadout.id, loadout.name);
      });
  }

  private refresh(): Observable<EquipmentLoadout[]> {
    const epoch = ++this.epoch;
    const session = this.events.logout();
    return this.api.get('equipment/loadouts').pipe(
      tap((loadouts) => {
        if (epoch !== this.epoch || session !== this.events.logout()) return;
        this.loadouts.set(loadouts);
        if (
          this.selectedId() &&
          !loadouts.some(
            (loadout: EquipmentLoadout) => loadout.id === this.selectedId(),
          )
        ) {
          this.selectedId.set(null);
          this.unsavedChanges.set(false);
        }
      }),
    );
  }

  load(): void {
    if (this.pending() || (this.selectionInitialized && this.busy())) return;
    this.run(
      forkJoin({
        loadouts: this.refresh(),
        equipment: this.equipment.refresh(),
      }).pipe(map((result) => result.loadouts)),
      (loadouts) => {
        if (this.selectionInitialized) return;
        // Restore a matching set without changing what the character is wearing.
        const matching = loadouts.find((loadout) =>
          this.matchesEquipped(loadout),
        );
        this.selectedId.set(matching?.id ?? null);
        this.selectionInitialized = true;
      },
    );
  }

  newLoadout(): void {
    if (this.busy() || this.unsavedChanges() || this.loadouts().filter(x => x.isUsable !== false).length >= this.limit())
      return;
    this.selectionInitialized = true;
    this.selectedId.set(null);
    this.error.set(null);
  }

  copyTo(targetId: string): void {
    const source = this.selected();
    if (!source || !targetId || this.busy()) return;
    this.run(this.api.post('equipment/loadouts/' + source.id + '/copy', { targetId }).pipe(
      tap(response => { if (!response.isSuccess) throw new Error(response.errorMessage ?? 'Preset could not be copied.'); }),
      switchMap(() => this.refresh())), () => this.error.set(null));
  }

  select(id: string): void {
    if (this.busy() || this.unsavedChanges() || id === this.selectedId())
      return;
    const loadout = this.loadouts().find((entry) => entry.id === id);
    if (!loadout) return;
    if (loadout.isUsable === false) { this.selectedId.set(id); this.error.set('This saved preset requires Nobility. Its equipment can still be viewed.'); return; }
    this.selectionInitialized = true;
    const session = this.events.logout();
    let applied = false;
    this.run(
      this.api.post('equipment/loadouts/' + id + '/apply', {}).pipe(
        tap(() => {
          applied = true;
        }),
        switchMap(() => {
          if (session !== this.events.logout()) return EMPTY;
          this.character.markOverviewDirty();
          this.inventory.load(true);
          return forkJoin({
            loadouts: this.refresh(),
            equipment: this.equipment.refresh(),
          }).pipe(map((result) => result.loadouts));
        }),
      ),
      () => this.selectedId.set(id),
      () => {
        // The apply may succeed even if the following refresh fails. Do not
        // leave the old set selected and autosave new equipment into it.
        if (applied) {
          this.selectedId.set(null);
          this.equipment.load(true);
        }
      },
    );
  }

  save(id: string | null, name: string): void {
    if (this.busy()) return;
    name = name.trim();
    if (
      !name ||
      name.length > 80 ||
      (id && id !== this.selectedId()) ||
      (!id && this.loadouts().filter(x => x.isUsable !== false).length >= this.limit()) ||
      (!!id && this.loadouts().some(x => x.id === id && x.isUsable === false))
    )
      return;
    this.saveCurrent(id, name);
  }

  retrySave(): void {
    const loadout = this.selected();
    if (this.busy() || !loadout || !this.unsavedChanges()) return;
    this.saveCurrent(loadout.id, loadout.name);
  }

  private saveCurrent(id: string | null, name: string): void {
    if (this.pending()) return;
    const previousIds = new Set(this.loadouts().map((loadout) => loadout.id));
    const session = this.events.logout();
    this.run(
      this.api
        .post('equipment/loadouts', { id, name })
        .pipe(
          switchMap(() =>
            session === this.events.logout() ? this.refresh() : EMPTY,
          ),
        ),
      (loadouts) => {
        const saved = id
          ? loadouts.find((loadout) => loadout.id === id)
          : loadouts.find((loadout) => !previousIds.has(loadout.id));
        if (saved) this.selectedId.set(saved.id);
        this.selectionInitialized = true;
        this.unsavedChanges.set(false);
      },
    );
  }

  remove(id: string): void {
    if (this.busy()) return;
    const session = this.events.logout();
    this.run(
      this.api
        .delete('equipment/loadouts/' + id)
        .pipe(
          switchMap(() =>
            session === this.events.logout() ? this.refresh() : EMPTY,
          ),
        ),
    );
  }

  setActivities(
    loadout: EquipmentLoadout,
    activity: EssenceCombatActivity,
    enabled: boolean,
  ): void {
    if (this.busy()) return;
    const activities = enabled
      ? [...loadout.autoUseActivities, activity]
      : loadout.autoUseActivities.filter((entry) => entry !== activity);
    const session = this.events.logout();
    this.run(
      this.api
        .put('equipment/loadouts/' + loadout.id + '/activities', { activities })
        .pipe(
          switchMap(() =>
            session === this.events.logout() ? this.refresh() : EMPTY,
          ),
        ),
    );
  }

  private matchesEquipped(loadout: EquipmentLoadout): boolean {
    if (
      loadout.slots.some(
        (slot) => !slot.equipmentInstanceId || !slot.equipmentInstance,
      )
    )
      return false;
    const saved = loadout.slots
      .map((slot) => slot.slotType + ':' + slot.equipmentInstanceId)
      .sort();
    const equipped = this.equipment
      .equipmentSlots()
      .filter((slot: EquipmentSlot) => slot.equipmentInstance)
      .map(
        (slot: EquipmentSlot) =>
          slot.equipmentSlotType + ':' + slot.equipmentInstance!.id,
      )
      .sort();
    return (
      saved.length === equipped.length &&
      saved.every((entry, index) => entry === equipped[index])
    );
  }

  private run(
    request: Observable<EquipmentLoadout[]>,
    onSuccess: (loadouts: EquipmentLoadout[]) => void = () => undefined,
    onError: () => void = () => undefined,
  ): void {
    if (this.pending()) return;
    const session = this.events.logout();
    this.pending.set(true);
    this.equipment.loadoutPending.set(true);
    this.error.set(null);
    request
      .pipe(
        finalize(() => {
          if (session !== this.events.logout()) return;
          this.pending.set(false);
          this.equipment.loadoutPending.set(false);
        }),
      )
      .subscribe({
        next: (loadouts) => {
          if (session === this.events.logout()) onSuccess(loadouts);
        },
        error: (error) => {
          if (session !== this.events.logout()) return;
          onError();
          this.error.set(
            error.errorMessage ??
              error.message ??
              'Could not update equipment loadout.',
          );
        },
      });
  }
}
