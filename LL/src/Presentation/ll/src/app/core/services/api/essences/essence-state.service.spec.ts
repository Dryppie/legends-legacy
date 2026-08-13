import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameEventService } from '../../real-time/game-event.service';
import { CharacterStateService } from '../character/character-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { QuestStateService } from '../quest/quest-state.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EssenceStateService } from './essence-state.service';
import { EssencesService } from './essences.service';
import {
  EssenceMutationResponseDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';

describe('EssenceStateService loadout drafts', () => {
  let service: EssenceStateService;
  let essences: jasmine.SpyObj<EssencesService>;
  const levelUpEnvelope = signal<any>(null);

  beforeEach(() => {
    levelUpEnvelope.set(null);
    essences = jasmine.createSpyObj<EssencesService>('EssencesService', [
      'getArchive',
      'getLoadouts',
      'getCreatureArchive',
      'getCodex',
      'saveLoadout',
      'updateLoadout',
      'activateLoadout',
      'spendDust',
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
          provide: GameEventService,
          useValue: {
            eventEnvelope: { CharacterLevelUpMsg: levelUpEnvelope },
          },
        },
        {
          provide: CharacterStateService,
          useValue: {
            currentCharacterId: signal('character-1'),
            markOverviewDirty: jasmine.createSpy(),
          },
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

  it('refreshes the creature archive when entering the Creatures view', () => {
    expect(essences.getCreatureArchive).toHaveBeenCalledTimes(1);

    service.setActiveView('creatures');

    expect(essences.getCreatureArchive).toHaveBeenCalledTimes(2);
  });

  it('still resets the draft during an ordinary post-mutation refresh', () => {
    service.setDraftSlot(0, 'essence-1');

    service.refresh();

    expect(service.draftSlots()).toEqual([null]);
    expect(service.hasDraftChanges()).toBeFalse();
  });

  it('live-refreshes loadouts when a level-up unlocks an Essence slot', () => {
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
        unlockedSlots: 2,
      }),
    );

    levelUpEnvelope.set({
      updateId: 'level-up-10',
      event: 'CharacterLevelUpMsg',
      payload: {
        characterId: 'character-1',
        level: 10,
        experience: 0,
        experienceUntilNextLevel: 100,
        unlockedEssenceSlots: 2,
      },
    });
    TestBed.flushEffects();

    expect(essences.getLoadouts).toHaveBeenCalledTimes(2);
    expect(service.loadouts()?.unlockedSlots).toBe(2);
    expect(service.draftSlots()).toEqual([null, null]);
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

  it('allows only one pending Essence Dust request', () => {
    const request = new Subject<EssenceMutationResponseDto>();
    const essence = { id: 'essence-1' } as PlayerEssenceDto;
    essences.spendDust.and.returnValue(request.asObservable());

    service.spendDust(essence);
    service.spendDust(essence);

    expect(essences.spendDust).toHaveBeenCalledOnceWith('essence-1', 1);
    expect(service.spendingDust()).toBeTrue();

    request.error(new Error('Request failed'));

    expect(service.spendingDust()).toBeFalse();
  });

  it('reconciles stale Dust state and shows the API validation message', () => {
    const essence = { id: 'essence-1' } as PlayerEssenceDto;
    essences.spendDust.and.returnValue(
      throwError(() => ({
        status: 400,
        errorMessage: 'Not enough Essence Dust.',
        message: 'Http failure response for /spend-dust: 400 OK',
      })),
    );
    essences.getArchive.calls.reset();
    essences.getArchive.and.returnValue(
      of({ essences: [essence], essenceDust: 0 }),
    );

    service.spendDust(essence);

    expect(essences.getArchive).toHaveBeenCalledTimes(1);
    expect(service.archive()?.essenceDust).toBe(0);
    expect(service.error()).toBe('Not enough Essence Dust.');
    expect(service.spendingDust()).toBeFalse();
  });
});
