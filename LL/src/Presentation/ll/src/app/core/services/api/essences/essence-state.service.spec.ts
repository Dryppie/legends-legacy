import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { QuestStateService } from '../quest/quest-state.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EssenceStateService } from './essence-state.service';
import { EssencesService } from './essences.service';

describe('EssenceStateService loadout drafts', () => {
  let service: EssenceStateService;
  let essences: jasmine.SpyObj<EssencesService>;

  beforeEach(() => {
    essences = jasmine.createSpyObj<EssencesService>('EssencesService', [
      'getArchive',
      'getLoadouts',
      'getCreatureArchive',
      'getCodex',
      'saveLoadout',
      'updateLoadout',
      'activateLoadout',
    ]);
    essences.getArchive.and.returnValue(of({ essences: [], essenceDust: 0 }));
    essences.getLoadouts.and.returnValue(
      of({
        loadouts: [
          {
            id: 'loadout-1',
            name: 'Default',
            isActive: true,
            slots: [],
          },
        ],
        limit: 3,
        unlockedSlots: 1,
      }),
    );
    essences.getCreatureArchive.and.returnValue(
      of({ creatures: [], canChangeEssenceFocus: true }),
    );
    essences.getCodex.and.returnValue(of({ entries: [] }));

    TestBed.configureTestingModule({
      providers: [
        EssenceStateService,
        { provide: EssencesService, useValue: essences },
        {
          provide: InventoryStateService,
          useValue: { items: signal([]), setInventory: jasmine.createSpy() },
        },
        { provide: EssenceItemViewService, useValue: {} },
        { provide: QuestStateService, useValue: {} },
        { provide: EventBusService, useValue: { logout: signal(false) } },
        {
          provide: CharacterStateService,
          useValue: { markOverviewDirty: jasmine.createSpy() },
        },
      ],
    });

    service = TestBed.inject(EssenceStateService);
    service.refresh();
  });

  it('preserves a dirty loadout draft during a route-entry refresh', () => {
    service.setDraftSlot(0, 'essence-1');

    service.refresh(true);

    expect(service.draftSlots()).toEqual(['essence-1']);
    expect(service.hasDraftChanges()).toBeTrue();
  });

  it('still resets the draft during an ordinary post-mutation refresh', () => {
    service.setDraftSlot(0, 'essence-1');

    service.refresh();

    expect(service.draftSlots()).toEqual([null]);
    expect(service.hasDraftChanges()).toBeFalse();
  });

  it('persists an equipped Essence immediately without enabling name save', () => {
    essences.updateLoadout.and.returnValue(
      of({
        id: 'loadout-1',
        name: 'Default',
        isActive: true,
        slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
      }),
    );
    service.setDraftSlot(0, 'essence-1');

    service.saveDraftSlots();

    expect(essences.updateLoadout).toHaveBeenCalledOnceWith('loadout-1', {
      id: 'loadout-1',
      name: 'Default',
      slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
    });
    expect(service.draftSlots()).toEqual(['essence-1']);
    expect(service.hasDraftChanges()).toBeFalse();
    expect(service.canSaveDraft()).toBeFalse();
  });

  it('creates a new loadout when its first Essence is equipped', () => {
    essences.saveLoadout.and.returnValue(
      of({
        id: 'loadout-2',
        name: 'New Loadout',
        isActive: false,
        slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
      }),
    );
    service.newLoadout();
    service.setDraftSlot(0, 'essence-1');

    service.saveDraftSlots();

    expect(essences.saveLoadout).toHaveBeenCalledOnceWith({
      id: null,
      name: 'New Loadout',
      slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
    });
    expect(service.selectedLoadoutId()).toBe('loadout-2');
    expect(service.loadouts()?.loadouts.length).toBe(2);
  });

  it('keeps a pending name edit separate from an automatic slot save', () => {
    essences.updateLoadout.and.returnValue(
      of({
        id: 'loadout-1',
        name: 'Default',
        isActive: true,
        slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
      }),
    );
    service.setDraftLoadoutName('Boss fights');
    service.setDraftSlot(0, 'essence-1');

    service.saveDraftSlots();

    expect(essences.updateLoadout).toHaveBeenCalledOnceWith('loadout-1', {
      id: 'loadout-1',
      name: 'Default',
      slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
    });
    expect(service.draftLoadoutName()).toBe('Boss fights');
    expect(service.canSaveDraft()).toBeTrue();
  });

  it('uses the manual save action only for a changed name', () => {
    essences.updateLoadout.and.returnValue(
      of({
        id: 'loadout-1',
        name: 'Boss fights',
        isActive: true,
        slots: [],
      }),
    );
    service.setDraftLoadoutName('Boss fights');

    expect(service.canSaveDraft()).toBeTrue();
    service.saveDraftLoadout();

    expect(essences.updateLoadout).toHaveBeenCalledOnceWith('loadout-1', {
      id: 'loadout-1',
      name: 'Boss fights',
      slots: [],
    });
    expect(service.canSaveDraft()).toBeFalse();
  });
});
