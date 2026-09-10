import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import {
  discardPeriodicTasks,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { Subject, of, throwError } from 'rxjs';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameRealtimeEventRegistry } from '../../real-time/game-realtime/game-realtime-event-registry.service';
import { CharacterStateService } from '../character/character-state.service';
import { EquipmentStateService } from '../equipment/equipment-state.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { VersionedMutationResult } from '../api.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { EssenceItemViewService } from './essence-item-view.service';
import { EssenceStateService } from './essence-state.service';
import { EssencesService } from './essences.service';
import {
  EssenceCodexEntryDto,
  CreatureArchiveEntryDto,
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
      creatureArchive: { creatures: [], canChangeCreatureFocus: true },
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
      'setCreatureFocus',
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
      of({ creatures: [], canChangeCreatureFocus: true }),
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

  it('keeps every Codex bonus visible at zero with no completed collections', () => {
    expect(service.codexBonusSummary().map((bonus) => [bonus.kind, bonus.percent]))
      .toEqual([
        ['EssenceDropRateRelativeBps', 0],
        ['FocusedMonsterEssenceDropRateRelativeBps', 0],
        ['EssenceExperienceGainBps', 0],
        ['EssencePityProgressionGainBps', 0],
      ]);
  });

  it('uses renamed Creature Focus timestamps for cooldowns, readiness and mutation responses', () => {
    const creature = {
      creatureId: 'creature-1', isCreatureFocus: false,
      essences: [{ essenceDefinitionId: 'essence-1' }],
    } as CreatureArchiveEntryDto;
    essences.getCreatureArchive.and.returnValue(of({
      creatures: [creature],
      canChangeCreatureFocus: false,
      creatureFocusAvailableAtUtc: new Date(Date.now() + 60_000).toISOString(),
      creatureFocusSetAtUtc: new Date(Date.now() - 7 * 60 * 60_000).toISOString(),
    }));
    service.refresh();
    expect(service.canChangeCreatureFocus()).toBeFalse();
    expect(service.creatureFocusReady()).toBeFalse();
    service.setCreatureFocus('creature-1');
    expect(essences.setCreatureFocus).not.toHaveBeenCalled();
    expect(service.error()).toContain('Creature Focus');
    essences.getCreatureArchive.and.returnValue(of({
      creatures: [creature], canChangeCreatureFocus: false,
      creatureFocusAvailableAtUtc: new Date(Date.now() - 60_000).toISOString(),
    }));
    service.refresh();
    expect(service.canChangeCreatureFocus()).toBeTrue();
    expect(service.creatureFocusReady()).toBeTrue();
    const updatedCreature = { ...creature, isCreatureFocus: true };
    essences.setCreatureFocus.and.returnValue(of(versionedMutation({
      creatureArchive: {
        creatures: [updatedCreature], canChangeCreatureFocus: false,
        creatureFocusAvailableAtUtc: new Date(Date.now() + 8 * 60 * 60_000).toISOString(),
        creatureFocusSetAtUtc: new Date(Date.now()).toISOString(),
      },
    })));
    service.setCreatureFocus('creature-1');
    expect(essences.setCreatureFocus).toHaveBeenCalledOnceWith('creature-1');
    expect(service.focusedCreature()?.creatureId).toBe('creature-1');
    expect(service.canChangeCreatureFocus()).toBeFalse();
    expect(service.creatureFocusReady()).toBeFalse();
  });

  it('totals only active Codex bonuses and updates after collection Ascensions', () => {
    const collection = (
      bonusKind: string, bonusValue: number, isUnlocked = true,
    ): EssenceCodexEntryDto => ({
      id: `collection-${bonusKind}-${bonusValue}`,
      title: 'Collection', description: '', benefitText: '', category: '',
      bonusKind, bonusValue, isUnlocked,
      baseBonusValue: 50, bonusValuePerCollectionAscensionTier: 10,
      collectionAscensionTier: 0, maxCollectionAscensionTier: 3,
      current: isUnlocked ? 2 : 1, required: 2, essences: [],
    });
    const entries = [
      collection('EssenceDropRateRelativeBps', 50),
      collection('EssenceDropRateRelativeBps', 75),
      collection('EssenceDropRateRelativeBps', 100, false),
      collection('FocusedMonsterEssenceDropRateRelativeBps', 75),
      collection('EssenceExperienceGainBps', 100),
      collection('EssencePityProgressionGainBps', 50, false),
    ];
    essences.getCodex.and.returnValue(of({ entries }));
    service.refresh();
    expect(service.codexBonusSummary().map((bonus) => bonus.percent))
      .toEqual([1.25, 0.75, 1, 0]);

    essences.getCodex.and.returnValue(of({ entries: [
      { ...entries[0], collectionAscensionTier: 3, bonusValue: 80 },
      ...entries.slice(1),
    ] }));
    service.refresh();
    expect(service.codexBonusSummary().map((bonus) => bonus.percent))
      .toEqual([1.55, 0.75, 1, 0]);

    essences.getCodex.and.returnValue(of({ entries: [] }));
    service.refresh();
    expect(service.codexBonusSummary().map((bonus) => bonus.percent))
      .toEqual([0, 0, 0, 0]);
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

  it('defers an unlocked Essence slot refresh until page entry', () => {
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

    expect(essences.getLoadouts).toHaveBeenCalledTimes(1);
    expect(service.dirty()).toBeTrue();
    service.refreshIfDirty();

    expect(essences.getLoadouts).toHaveBeenCalledTimes(2);
    expect(service.loadouts()?.unlockedSlots).toBe(2);
    expect(service.draftSlots()).toEqual([null, null]);
  });

  it('coalesces combat invalidations into one fetch on page entry', fakeAsync(() => {
    const sync = TestBed.inject(StateSyncCoordinator);
    for (let revision = 1; revision <= 5; revision++) {
      sync.acceptInvalidations({ essences: revision });
      tick(60);
    }

    expect(service.dirty()).toBeTrue();
    expect(essences.getArchive).toHaveBeenCalledTimes(1);
    expect(essences.getLoadouts).toHaveBeenCalledTimes(1);
    expect(essences.getCreatureArchive).toHaveBeenCalledTimes(1);
    expect(essences.getCodex).toHaveBeenCalledTimes(1);

    essences.getArchive.and.returnValue(of({
      essences: [{ id: 'essence-1', currentXp: 129600 } as PlayerEssenceDto],
      essenceDust: 0,
    }));
    service.refreshIfDirty();
    tick(60);

    expect(service.archive()?.essences[0].currentXp).toBe(129600);
    expect(service.dirty()).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    expect(essences.getLoadouts).toHaveBeenCalledTimes(2);
    service.refreshIfDirty();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    discardPeriodicTasks();
  }));

  it('sees a pending invalidation on entry without an extra fetch after its callback', fakeAsync(() => {
    TestBed.inject(StateSyncCoordinator).acceptInvalidations({ essences: 1 });
    service.refreshIfDirty();
    tick(60);

    expect(service.dirty()).toBeFalse();
    service.refreshIfDirty();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    discardPeriodicTasks();
  }));

  it('keeps an in-flight combat update dirty for the next entry without fetching again', fakeAsync(() => {
    const sync = TestBed.inject(StateSyncCoordinator);
    sync.acceptInvalidations({ essences: 1 });
    tick(60);
    const archiveResponse = new Subject<any>();
    essences.getArchive.and.returnValue(archiveResponse);
    service.refreshIfDirty();
    service.refreshIfDirty();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);

    sync.acceptInvalidations({ essences: 2 });
    tick(60);
    archiveResponse.next({ essences: [], essenceDust: 0 });
    archiveResponse.complete();
    tick(60);

    expect(service.dirty()).toBeTrue();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    essences.getArchive.and.returnValue(of({ essences: [], essenceDust: 0 }));
    service.refreshIfDirty();
    expect(essences.getArchive).toHaveBeenCalledTimes(3);
    expect(service.dirty()).toBeFalse();
    discardPeriodicTasks();
  }));

  it('cleans an async fetch when a delayed invalidation was already known at request time', fakeAsync(() => {
    TestBed.inject(StateSyncCoordinator).acceptInvalidations({ essences: 1 });
    const archiveResponse = new Subject<any>();
    essences.getArchive.and.returnValue(archiveResponse);
    service.refreshIfDirty();
    tick(60);
    archiveResponse.next({ essences: [], essenceDust: 0 });
    archiveResponse.complete();
    service.refreshIfDirty();

    expect(service.dirty()).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    discardPeriodicTasks();
  }));

  it('keeps a failed refresh dirty and retries only on page entry', fakeAsync(() => {
    TestBed.inject(StateSyncCoordinator).acceptInvalidations({ essences: 1 });
    tick(60);
    essences.getArchive.and.returnValue(throwError(() => new Error('Unavailable')));
    service.refreshIfDirty();
    tick(1000);
    expect(service.dirty()).toBeTrue();
    expect(service.loading()).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);

    essences.getArchive.and.returnValue(of({ essences: [], essenceDust: 0 }));
    service.refreshIfDirty();
    expect(service.dirty()).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(3);
    discardPeriodicTasks();
  }));

  it('uses an authoritative mutation response to clean the dirty cache', fakeAsync(() => {
    const sync = TestBed.inject(StateSyncCoordinator);
    sync.acceptInvalidations({ essences: 1 });
    tick(60);
    essences.spendDust.and.returnValue(of(versionedMutation({}, { essences: 2 })));
    service.spendDust({ id: 'essence-1' } as PlayerEssenceDto);
    sync.acceptInvalidations({ essences: 2 });
    tick(60);
    service.refreshIfDirty();

    expect(service.dirty()).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(1);
    discardPeriodicTasks();
  }));

  it('allows a later dirty refresh when a mutation supersedes an in-flight fetch', fakeAsync(() => {
    const sync = TestBed.inject(StateSyncCoordinator);
    sync.acceptInvalidations({ essences: 1 });
    const archiveResponse = new Subject<any>();
    essences.getArchive.and.returnValue(archiveResponse);
    service.refreshIfDirty();
    essences.spendDust.and.returnValue(of(versionedMutation({}, { essences: 2 })));
    service.spendDust({ id: 'essence-1' } as PlayerEssenceDto);
    archiveResponse.next({ essences: [], essenceDust: 999 });
    archiveResponse.complete();

    expect(service.archive()?.essenceDust).toBe(0);
    expect(service.loading()).toBeFalse();
    sync.acceptInvalidations({ essences: 3 });
    tick(60);
    essences.getArchive.and.returnValue(of({ essences: [], essenceDust: 0 }));
    service.refreshIfDirty();
    expect(essences.getArchive).toHaveBeenCalledTimes(3);
    expect(service.dirty()).toBeFalse();
    discardPeriodicTasks();
  }));

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

  function prepareConduitLoadouts() {
    const archive = {
      essences: [
        { id: 'passive', isChanneledEssenceEligible: false },
        { id: 'channeled-essence', isChanneledEssenceEligible: true },
        { id: 'other', isChanneledEssenceEligible: true },
        { id: 'unequipped', isChanneledEssenceEligible: true },
      ] as PlayerEssenceDto[],
      essenceDust: 0,
    };
    const loadouts = {
      unlockedSlots: 5,
      limit: 3,
      loadouts: [
        {
          id: 'loadout-1', name: 'Default', autoUseActivities: [],
          slots: [
            { slotIndex: 1, playerEssenceId: 'passive' },
            { slotIndex: 3, playerEssenceId: 'channeled-essence' },
            { slotIndex: 4, playerEssenceId: 'other' },
          ],
        },
        {
          id: 'loadout-2', name: 'Dungeon', autoUseActivities: ['Dungeon'],
          slots: [{ slotIndex: 2, playerEssenceId: 'other' }],
        },
      ] as EssenceLoadoutDto[],
    };
    essences.getArchive.and.returnValue(of(archive));
    essences.getLoadouts.and.returnValue(of(loadouts));
    service.refresh();
    return { archive, loadouts };
  }

  it('finds the first occupied slot without skipping an ineligible Essence and follows the selected loadout', () => {
    const { loadouts } = prepareConduitLoadouts();
    expect(service.firstOccupiedDraftSlot()).toBe(1);
    expect(service.draftSlots()[service.firstOccupiedDraftSlot()]).toBe('passive');
    service.selectLoadout(loadouts.loadouts[1]);
    expect(service.firstOccupiedDraftSlot()).toBe(2);
    expect(service.draftSlots()[service.firstOccupiedDraftSlot()]).toBe('other');
    service.newLoadout();
    expect(service.firstOccupiedDraftSlot()).toBe(-1);
    expect(service.draftSlots()).toEqual([null, null, null, null, null]);
    expect(essences.updateLoadout).not.toHaveBeenCalled();
  });

  it('makes an equipped Essence the Channeled Essence with one atomic swap and uses the returned state', () => {
    const { archive, loadouts } = prepareConduitLoadouts();
    const response = new Subject<VersionedMutationResult<EssenceMutationResponseDto>>();
    essences.updateLoadout.and.returnValue(response);
    service.channelEssence('channeled-essence');
    expect(service.draftSlots()).toEqual([null, 'channeled-essence', null, 'passive', 'other']);
    expect(service.savingLoadout()).toBeTrue();
    service.channelEssence('other');
    const savedLoadout = {
      ...loadouts.loadouts[0],
      slots: [
        { slotIndex: 1, playerEssenceId: 'channeled-essence' },
        { slotIndex: 3, playerEssenceId: 'passive' },
        { slotIndex: 4, playerEssenceId: 'other' },
      ],
    };
    expect(essences.updateLoadout).toHaveBeenCalledOnceWith('loadout-1', {
      id: 'loadout-1', name: 'Default', slots: savedLoadout.slots,
    });
    response.next(versionedMutation({
      savedLoadout,
      archive: { ...archive, essences: archive.essences.map((essence) =>
        essence.id === 'other' ? { ...essence, isChanneledEssenceEligible: false } : essence,
      ) },
      loadouts: { ...loadouts, loadouts: [savedLoadout, loadouts.loadouts[1]] },
    }));
    expect(service.savingLoadout()).toBeFalse();
    expect(service.hasDraftChanges()).toBeFalse();
    expect(service.draftSlots()).toEqual([null, 'channeled-essence', null, 'passive', 'other']);
    expect(service.canChannelEssence('other')).toBeFalse();
    expect(essences.getArchive).toHaveBeenCalledTimes(2);
    expect(essences.saveLoadout).not.toHaveBeenCalled();
    service.selectLoadout(loadouts.loadouts[1]);
    expect(service.draftSlots()).toEqual([null, null, 'other', null, null]);
    service.selectLoadout(service.loadouts()!.loadouts[0]);
    expect(service.draftSlots()[1]).toBe('channeled-essence');
  });

  it('restores every slot after a failed Channeled Essence swap while preserving an unsaved loadout name', () => {
    prepareConduitLoadouts();
    const response = new Subject<VersionedMutationResult<EssenceMutationResponseDto>>();
    essences.updateLoadout.and.returnValue(response);
    service.setDraftLoadoutName('New name');
    service.channelEssence('channeled-essence');
    expect(service.draftSlots()[1]).toBe('channeled-essence');
    response.error(new Error('Loadout could not be saved.'));
    expect(service.draftSlots()).toEqual([null, 'passive', null, 'channeled-essence', 'other']);
    expect(service.firstOccupiedDraftSlot()).toBe(1);
    expect(service.draftLoadoutName()).toBe('New name');
    expect(service.canSaveDraft()).toBeTrue();
    expect(service.savingLoadout()).toBeFalse();
    expect(service.error()).toBe('Loadout could not be saved.');
    expect(essences.updateLoadout).toHaveBeenCalledTimes(1);
  });

  it('does not move an ineligible, absent, current or unequipped Channeled Essence', () => {
    prepareConduitLoadouts();
    for (const id of ['passive', 'missing', 'unequipped']) {
      expect(service.canChannelEssence(id)).toBeFalse();
      service.channelEssence(id);
    }
    service.setDraftSlot(1, 'channeled-essence');
    expect(service.canChannelEssence('channeled-essence')).toBeFalse();
    service.channelEssence('channeled-essence');
    service.newLoadout();
    service.channelEssence('other');
    expect(service.draftSlots()).toEqual([null, null, null, null, null]);
    expect(essences.updateLoadout).not.toHaveBeenCalled();
    expect(essences.saveLoadout).not.toHaveBeenCalled();
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

  it('shows the server rejection reason and restores slots when equipping during a dungeon run', () => {
    const message = 'Finish your dungeon run before changing your combat build.';
    const error = Object.assign(
      new HttpErrorResponse({
        status: 400,
        statusText: 'OK',
        url: '/api/v1/essence/loadouts/loadout-1',
        error: { detail: message },
      }),
      { errorMessage: message },
    );
    essences.updateLoadout.and.returnValue(throwError(() => error));
    service.setDraftLoadoutName('Boss fights');
    service.setDraftSlot(0, 'essence-1');

    service.saveDraftSlots();

    expect(essences.updateLoadout).toHaveBeenCalledTimes(1);
    expect(service.error()).toBe(message);
    expect(service.draftSlots()).toEqual([null]);
    expect(service.draftLoadoutName()).toBe('Boss fights');
    expect(service.savingLoadout()).toBeFalse();
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

  for (const [dust, expected] of [[25, 25], [100, 30], [0, 0]]) {
    it('caps Max upgrading with ' + dust + ' dust at available levels and balance', () => {
      essences.getArchive.and.returnValue(of({ essences: [], essenceDust: dust }));
      service.refresh(true);
      const request = new Subject<VersionedMutationResult<EssenceMutationResponseDto>>();
      essences.spendDust.and.returnValue(request.asObservable());
      service.spendDust({ id: 'essence-1', level: 30, levelCap: 60 } as PlayerEssenceDto, true);
      if (expected > 0) {
        expect(essences.spendDust).toHaveBeenCalledOnceWith('essence-1', expected);
        request.complete();
      } else expect(essences.spendDust).not.toHaveBeenCalled();
    });
  }

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
            canChangeCreatureFocus: false,
          },
        }),
      ),
    );

    service.spendDust(upgradedEssence);

    expect(service.archive()?.essences[0].level).toBe(2);
    expect(service.archive()?.essenceDust).toBe(9);
    expect(service.loadouts()?.unlockedSlots).toBe(2);
    expect(service.creatureArchive()?.canChangeCreatureFocus).toBeFalse();
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
