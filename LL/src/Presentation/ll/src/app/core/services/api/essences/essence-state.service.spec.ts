import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { CharacterStateService } from '../character/character-state.service';
import { EquipmentStateService } from '../equipment/equipment-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { VersionedMutationResult } from '../api.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EssenceStateService } from './essence-state.service';
import { EssencesService } from './essences.service';
import {
  EssenceLoadoutDto,
  EssenceMutationResponseDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';

describe('EssenceStateService loadout drafts', () => {
  let service: EssenceStateService;
  let essences: jasmine.SpyObj<EssencesService>;
  const levelUpEnvelope = signal<any>(null);

  const versionedMutation = (
    overrides: Partial<EssenceMutationResponseDto> = {},
    domainVersions: Readonly<Record<string, number>> = {
      essences: 1,
      inventory: 1,
      equipment: 1,
    },
  ): VersionedMutationResult<EssenceMutationResponseDto> => ({
    data: {
      succeeded: true,
      message: 'Essence updated.',
      archive: { essences: [], essenceDust: 0 },
      loadouts: {
        loadouts: [
          {
            id: 'loadout-1',
            name: 'Default',
            autoUseActivities: [],
            slots: [],
          },
        ],
        limit: 3,
        unlockedSlots: 1,
      },
      creatureArchive: { creatures: [], canChangeEssenceFocus: true },
      codex: { entries: [] },
      inventoryItems: [],
      equipmentSlots: [],
      ...overrides,
    },
    domainVersions,
  });

  const versionedLoadout = (
    loadout: Omit<EssenceLoadoutDto, 'autoUseActivities'> &
      Partial<Pick<EssenceLoadoutDto, 'autoUseActivities'>>,
  ): VersionedMutationResult<EssenceMutationResponseDto> => {
    const resolvedLoadout: EssenceLoadoutDto = {
      autoUseActivities: [],
      ...loadout,
    };
    return versionedMutation(
      {
        savedLoadout: resolvedLoadout,
        loadouts: {
          loadouts:
            loadout.id === 'loadout-1'
              ? [resolvedLoadout]
              : [
                  {
                    id: 'loadout-1',
                    name: 'Default',
                    autoUseActivities: [],
                    slots: [],
                  },
                  resolvedLoadout,
                ],
          limit: 3,
          unlockedSlots: 1,
        },
      },
      { essences: 1 },
    );
  };

  beforeEach(() => {
    levelUpEnvelope.set(null);
    essences = jasmine.createSpyObj<EssencesService>('EssencesService', [
      'getArchive',
      'getLoadouts',
      'getCreatureArchive',
      'getCodex',
      'saveLoadout',
      'updateLoadout',
      'spendDust',
      'dismantle',
    ]);
    essences.getArchive.and.returnValue(of({ essences: [], essenceDust: 0 }));
    essences.getLoadouts.and.returnValue(
      of({
        loadouts: [
          {
            id: 'loadout-1',
            name: 'Default',
            autoUseActivities: [],
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
          useValue: {
            items: signal([]),
            setInventory: jasmine.createSpy(),
            applyVersionedInventory: jasmine.createSpy().and.returnValue(true),
          },
        },
        {
          provide: EquipmentStateService,
          useValue: { setSlots: jasmine.createSpy() },
        },
        { provide: EssenceItemViewService, useValue: {} },
        { provide: EventBusService, useValue: { logout: signal(false) } },
        {
          provide: GameRealtimeEventRegistry,
          useValue: {
            eventEnvelope: { CharacterLevelUp: levelUpEnvelope },
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
            autoUseActivities: [],
            slots: [],
          },
        ],
        limit: 3,
        unlockedSlots: 2,
      }),
    );

    levelUpEnvelope.set({
      updateId: 'level-up-10',
      event: 'CharacterLevelUp',
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
      of(
        versionedLoadout({
          id: 'loadout-1',
          name: 'Default',
          slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
        }),
      ),
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

  it('uses the returned Archive after re-saving the default loadout', () => {
    essences.updateLoadout.and.returnValue(
      of(
        versionedLoadout({
          id: 'loadout-1',
          name: 'Default',
          slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
        }),
      ),
    );
    expect(essences.getArchive).toHaveBeenCalledTimes(1);

    service.setDraftSlot(0, 'essence-1');
    service.saveDraftSlots();

    expect(essences.getArchive).toHaveBeenCalledTimes(1);
  });

  it('uses the mutation state without refetching the Archive for a new loadout', () => {
    essences.saveLoadout.and.returnValue(
      of(
        versionedLoadout({
          id: 'loadout-2',
          name: 'New Loadout',
          slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
        }),
      ),
    );
    service.newLoadout();
    service.setDraftSlot(0, 'essence-1');

    service.saveDraftSlots();

    expect(essences.getArchive).toHaveBeenCalledTimes(1);
  });

  it('creates a new loadout when its first Essence is equipped', () => {
    essences.saveLoadout.and.returnValue(
      of(
        versionedLoadout({
          id: 'loadout-2',
          name: 'New Loadout',
          slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
        }),
      ),
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
      of(
        versionedLoadout({
          id: 'loadout-1',
          name: 'Default',
          slots: [{ slotIndex: 0, playerEssenceId: 'essence-1' }],
        }),
      ),
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
      of(
        versionedLoadout({
          id: 'loadout-1',
          name: 'Boss fights',
          slots: [],
        }),
      ),
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
    const request = new Subject<
      VersionedMutationResult<EssenceMutationResponseDto>
    >();
    const essence = { id: 'essence-1' } as PlayerEssenceDto;
    essences.spendDust.and.returnValue(request.asObservable());

    service.spendDust(essence);
    service.spendDust(essence);

    expect(essences.spendDust).toHaveBeenCalledOnceWith('essence-1', 1);
    expect(service.spendingDust()).toBeTrue();

    request.error(new Error('Request failed'));

    expect(service.spendingDust()).toBeFalse();
  });

  it('shatters checked Essence stacks sequentially with their quantities', () => {
    essences.dismantle.and.returnValues(
      of(versionedMutation({}, { essences: 2, inventory: 2, equipment: 2 })),
      of(versionedMutation({}, { essences: 3, inventory: 3, equipment: 3 })),
    );
    let response: EssenceMutationResponseDto | undefined;

    service
      .dismantleInventoryEssences([
        { inventoryItemId: 'inventory-1', quantity: 5 },
        { inventoryItemId: 'inventory-2', quantity: 3 },
      ])
      ?.subscribe((result) => (response = result));

    expect(essences.dismantle.calls.allArgs()).toEqual([
      ['inventory-1', 5],
      ['inventory-2', 3],
    ]);
    expect(response?.succeeded).toBeTrue();
  });

  it('applies a Dust upgrade response without reloading companion archives', () => {
    const upgradedEssence = { id: 'essence-1', level: 2 } as PlayerEssenceDto;
    essences.getCreatureArchive.calls.reset();
    essences.getCodex.calls.reset();
    essences.spendDust.and.returnValue(
      of(
        versionedMutation({
          message: 'Essence Dust spent.',
          archive: { essences: [upgradedEssence], essenceDust: 9 },
          loadouts: {
            loadouts: [
              {
                id: 'loadout-1',
                name: 'Default',
                autoUseActivities: [],
                slots: [],
              },
            ],
            limit: 3,
            unlockedSlots: 2,
          },
          creatureArchive: {
            creatures: [],
            canChangeEssenceFocus: false,
          },
        }),
      ),
    );

    service.spendDust(upgradedEssence);

    expect(service.archive()?.essences[0].level).toBe(2);
    expect(service.archive()?.essenceDust).toBe(9);
    expect(service.loadouts()?.unlockedSlots).toBe(2);
    expect(service.creatureArchive()?.canChangeEssenceFocus).toBeFalse();
    expect(
      TestBed.inject(InventoryStateService).applyVersionedInventory,
    ).toHaveBeenCalled();
    expect(TestBed.inject(EquipmentStateService).setSlots).toHaveBeenCalledWith(
      [],
    );
    expect(essences.getCreatureArchive).not.toHaveBeenCalled();
    expect(essences.getCodex).not.toHaveBeenCalled();
  });

  it('ignores an Essence snapshot older than the latest observed version', () => {
    const upgradedEssence = { id: 'essence-1', level: 2 } as PlayerEssenceDto;
    TestBed.inject(DomainVersionTracker).observe({ essences: 3 });
    essences.spendDust.and.returnValue(
      of(
        versionedMutation(
          {
            archive: { essences: [upgradedEssence], essenceDust: 9 },
          },
          { essences: 2, inventory: 4, equipment: 4 },
        ),
      ),
    );

    service.spendDust(upgradedEssence);

    expect(service.archive()?.essences).toEqual([]);
    expect(service.archive()?.essenceDust).toBe(0);
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
