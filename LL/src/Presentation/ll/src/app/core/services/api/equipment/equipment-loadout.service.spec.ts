import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import {
  EquipmentLoadout,
  EquipmentLoadoutService,
} from './equipment-loadout.service';
import { ApiService, VersionedMutationResult } from '../api.service';
import { EquipmentStateService } from './equipment-state.service';
import { EquipmentChangeResponse, EquipmentService } from './equipment.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterStateService } from '../character/character-state.service';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import {
  EquipmentSlot,
  EquipmentSlotType,
} from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentInstance } from '../../../../shared/models/item';

describe('EquipmentLoadoutService', () => {
  let state: EquipmentLoadoutService;
  let equipment: EquipmentStateService;
  let request: Subject<unknown>;
  let equipmentRequest: Subject<
    VersionedMutationResult<EquipmentChangeResponse>
  >;
  let api: jasmine.SpyObj<ApiService>;
  let equipmentApi: jasmine.SpyObj<EquipmentService>;
  let inventory: jasmine.SpyObj<InventoryStateService>;
  let character: jasmine.SpyObj<CharacterStateService>;
  let presets: EquipmentLoadout[];
  let currentSlots: EquipmentSlot[];
  const logout = signal(0);
  const staff = {
    id: 'staff',
    displayName: 'Arcane Staff',
  } as EquipmentInstance;
  const helm = { id: 'helm', displayName: 'Heavy Helm' } as EquipmentInstance;
  const slot = (
    item: EquipmentInstance,
    type = EquipmentSlotType.MainHand,
  ): EquipmentSlot => ({
    id: type,
    iconPath: '',
    equipmentSlotType: type,
    equipmentInstance: item,
  });
  const preset = (
    id: string,
    name: string,
    slots: EquipmentSlot[],
  ): EquipmentLoadout => ({
    id,
    name,
    autoUseActivities: [],
    slots: slots.map((entry) => ({
      slotType: entry.equipmentSlotType,
      equipmentInstanceId: entry.equipmentInstance!.id,
      equipmentInstance: entry.equipmentInstance!,
    })),
  });

  beforeEach(() => {
    logout.set(0);
    currentSlots = [slot(staff)];
    presets = [
      preset('starter', 'Starter', currentSlots),
      preset('dungeon', 'Dungeons', [slot(helm, EquipmentSlotType.Head)]),
    ];
    request = new Subject();
    equipmentRequest = new Subject();
    api = jasmine.createSpyObj('ApiService', ['get', 'post', 'put', 'delete']);
    api.get.and.callFake(() => of([...presets]));
    api.post.and.returnValue(request);
    api.put.and.returnValue(request);
    api.delete.and.returnValue(request);
    equipmentApi = jasmine.createSpyObj('EquipmentService', [
      'getEquipment',
      'equipEquipment',
      'unequipEquipment',
    ]);
    equipmentApi.getEquipment.and.callFake(() => of(currentSlots));
    equipmentApi.equipEquipment.and.returnValue(equipmentRequest);
    equipmentApi.unequipEquipment.and.returnValue(equipmentRequest);
    inventory = jasmine.createSpyObj('InventoryStateService', [
      'load',
      'setEquippedItems',
      'applyVersionedInventory',
    ]);
    character = jasmine.createSpyObj('CharacterStateService', [
      'markOverviewDirty',
    ]);
    TestBed.configureTestingModule({
      providers: [
        EquipmentLoadoutService,
        EquipmentStateService,
        { provide: ApiService, useValue: api },
        { provide: EquipmentService, useValue: equipmentApi },
        { provide: InventoryStateService, useValue: inventory },
        { provide: CharacterStateService, useValue: character },
        { provide: EventBusService, useValue: { logout } },
        {
          provide: StateSyncCoordinator,
          useValue: { register: () => undefined },
        },
        { provide: DomainVersionTracker, useValue: { isCurrent: () => true } },
      ],
    });
    equipment = TestBed.inject(EquipmentStateService);
    state = TestBed.inject(EquipmentLoadoutService);
    TestBed.flushEffects();
    state.load();
    api.get.calls.reset();
    equipmentApi.getEquipment.calls.reset();
  });

  function completeLoadoutRequest(): void {
    request.next(true);
    request.complete();
  }

  function completeEquipmentChange(slots: EquipmentSlot[]): void {
    currentSlots = slots;
    equipmentRequest.next({
      data: { equipmentSlots: slots, inventoryItems: [] },
      domainVersions: { equipment: 1 },
    });
    equipmentRequest.complete();
  }

  it('restores a matching set without changing equipment or saving it on page entry', () => {
    expect(state.selectedId()).toBe('starter');
    expect(api.post).not.toHaveBeenCalled();
    expect(equipment.equipmentSlots()).toEqual([slot(staff)]);
    state.load();
    expect(state.selectedId()).toBe('starter');
    expect(api.post).not.toHaveBeenCalled();
  });

  it('equips a selected set and changes selection only after the equipped pane refreshes', () => {
    const refresh = new Subject<EquipmentSlot[]>();
    equipmentApi.getEquipment.and.returnValue(refresh);
    state.select('dungeon');
    expect(api.post).toHaveBeenCalledOnceWith(
      'equipment/loadouts/dungeon/apply',
      {},
    );
    expect(state.selectedId()).toBe('starter');
    expect(equipment.loading()).toBeTrue();
    completeLoadoutRequest();
    expect(state.pending()).toBeTrue();
    expect(state.selectedId()).toBe('starter');

    currentSlots = [slot(helm, EquipmentSlotType.Head)];
    refresh.next(currentSlots);
    refresh.complete();
    expect(state.selectedId()).toBe('dungeon');
    expect(equipment.equipmentSlots()).toEqual(currentSlots);
    expect(inventory.load).toHaveBeenCalledWith(true);
    expect(character.markOverviewDirty).toHaveBeenCalled();
    expect(state.pending()).toBeFalse();
    expect(equipment.loading()).toBeFalse();
    expect(api.post.calls.count()).toBe(1);
  });

  it('keeps the current set selected when the target loadout cannot be equipped', () => {
    state.select('dungeon');
    request.error({ errorMessage: 'Missing equipment' });
    expect(state.selectedId()).toBe('starter');
    expect(equipment.equipmentSlots()).toEqual([slot(staff)]);
    expect(state.error()).toBe('Missing equipment');
    expect(equipmentApi.getEquipment).not.toHaveBeenCalled();
    expect(state.busy()).toBeFalse();
  });

  it('detaches the old set when applying succeeds but the following refresh fails', () => {
    state.select('dungeon');
    api.get.and.returnValue(throwError(() => ({ message: 'Refresh failed' })));
    completeLoadoutRequest();
    expect(state.selectedId()).toBeNull();
    expect(state.error()).toBe('Refresh failed');
    api.post.calls.reset();
    equipment.equip(helm, EquipmentSlotType.Head);
    completeEquipmentChange([slot(helm, EquipmentSlotType.Head)]);
    expect(api.post).not.toHaveBeenCalled();
  });

  it('automatically saves a successful equip to the selected set and serializes subsequent actions', () => {
    equipment.equip(helm, EquipmentSlotType.Head);
    expect(api.post).not.toHaveBeenCalled();
    state.select('dungeon');
    expect(api.post).not.toHaveBeenCalled();
    completeEquipmentChange([slot(staff), slot(helm, EquipmentSlotType.Head)]);
    expect(api.post).toHaveBeenCalledOnceWith('equipment/loadouts', {
      id: 'starter',
      name: 'Starter',
    });
    expect(state.unsavedChanges()).toBeTrue();
    expect(equipment.loading()).toBeTrue();

    state.select('dungeon');
    equipment.unequip(EquipmentSlotType.Head);
    expect(api.post.calls.count()).toBe(1);
    expect(equipmentApi.unequipEquipment).not.toHaveBeenCalled();
    presets[0] = preset('starter', 'Starter', currentSlots);
    completeLoadoutRequest();
    expect(state.unsavedChanges()).toBeFalse();
    expect(state.selectedId()).toBe('starter');
    expect(equipment.loading()).toBeFalse();

    equipment.load(true);
    expect(api.post.calls.count()).toBe(1);
  });

  it('automatically saves empty slots after unequipping', () => {
    equipment.unequip(EquipmentSlotType.MainHand);
    completeEquipmentChange([]);
    expect(api.post).toHaveBeenCalledOnceWith('equipment/loadouts', {
      id: 'starter',
      name: 'Starter',
    });
    presets[0] = preset('starter', 'Starter', []);
    completeLoadoutRequest();
    expect(state.selected()?.slots).toEqual([]);
    expect(equipment.equipmentSlots()).toEqual([]);
  });

  it('does not save a failed equipment change', () => {
    equipment.equip(helm, EquipmentSlotType.Head);
    equipmentRequest.error({ message: 'Cannot equip' });
    expect(api.post).not.toHaveBeenCalled();
    expect(state.unsavedChanges()).toBeFalse();
    expect(equipment.equipmentSlots()).toEqual([slot(staff)]);
  });

  it('retains unsaved changes for retry and prevents switching before they are saved', () => {
    equipment.unequip(EquipmentSlotType.MainHand);
    completeEquipmentChange([]);
    request.error({ message: 'Save failed' });
    expect(state.unsavedChanges()).toBeTrue();
    expect(state.error()).toBe('Save failed');
    state.select('dungeon');
    state.newLoadout();
    expect(state.selectedId()).toBe('starter');
    expect(api.post.calls.count()).toBe(1);

    request = new Subject();
    api.post.and.returnValue(request);
    state.retrySave();
    expect(api.post).toHaveBeenCalledWith('equipment/loadouts', {
      id: 'starter',
      name: 'Starter',
    });
    presets[0] = preset('starter', 'Starter', []);
    completeLoadoutRequest();
    expect(state.unsavedChanges()).toBeFalse();
    expect(state.error()).toBeNull();
  });

  it('creates a new active set from current equipment without overwriting the previous set', () => {
    state.newLoadout();
    expect(state.selectedId()).toBeNull();
    equipment.equip(helm, EquipmentSlotType.Head);
    completeEquipmentChange([slot(helm, EquipmentSlotType.Head)]);
    expect(api.post).not.toHaveBeenCalled();

    state.save(null, 'New set');
    presets.push(preset('new', 'New set', currentSlots));
    completeLoadoutRequest();
    expect(state.selectedId()).toBe('new');
    expect(presets[0].slots[0].equipmentInstanceId).toBe('staff');
  });

  it('deleting the selected set keeps gear equipped and leaves other sets unchanged', () => {
    state.remove('starter');
    presets = presets.filter((loadout) => loadout.id !== 'starter');
    completeLoadoutRequest();
    expect(state.selectedId()).toBeNull();
    expect(equipment.equipmentSlots()).toEqual([slot(staff)]);
    expect(api.post).not.toHaveBeenCalled();
  });

  it('does not trigger an equipment save on an activity update', () => {
    state.setActivities(presets[0], 'Dungeon', true);
    expect(api.put).toHaveBeenCalledOnceWith(
      'equipment/loadouts/starter/activities',
      { activities: ['Dungeon'] },
    );
    completeLoadoutRequest();
    expect(api.post).not.toHaveBeenCalled();
    expect(state.selectedId()).toBe('starter');
  });

  it('ignores a loadout switch that finishes after logout', () => {
    state.select('dungeon');
    logout.set(1);
    TestBed.flushEffects();
    completeLoadoutRequest();
    expect(api.get).not.toHaveBeenCalled();
    expect(state.loadouts()).toEqual([]);
    expect(state.selectedId()).toBeNull();
    expect(state.pending()).toBeFalse();
    expect(equipment.loading()).toBeFalse();
  });

  it('ignores an equipment response after logout and never autosaves it', () => {
    equipment.equip(helm, EquipmentSlotType.Head);
    logout.set(1);
    TestBed.flushEffects();
    completeEquipmentChange([slot(helm, EquipmentSlotType.Head)]);
    expect(api.post).not.toHaveBeenCalled();
    expect(equipment.equipmentSlots()).toEqual([]);
  });
});
