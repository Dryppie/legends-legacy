import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Observable, of, Subject } from 'rxjs';
import { Guild } from '../../../../shared/models/Dtos/guild/guild';
import { GuildMissionOverview } from '../../../../shared/models/Dtos/guild/guildMission';
import {
  GuildStateService,
  isHandledGuildInitiatorEcho,
  normalizeGuild,
  normalizeGuildMissionOverview,
} from './guild-state.service';
import { StateSyncRefresh } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';

describe('normalizeGuild', () => {
  it('defaults guild-vault fields omitted by an older API response', () => {
    const guild = {
      id: 'guild-id',
      name: 'Guild',
      tag: 'TAG',
      guildXp: 0,
      guildLevel: 1,
      members: [],
      maxMembers: 10,
      invites: [],
      resources: [],
    } as never;

    const normalized = normalizeGuild(guild);

    expect(normalized?.rolePermissions).toEqual([]);
    expect(normalized?.vaultItems).toEqual([]);
  });
});

describe('normalizeGuildMissionOverview', () => {
  it('rejects mission state returned for a previous guild', () => {
    const missions = createOverview('old-guild');

    expect(normalizeGuildMissionOverview(missions, 'new-guild')).toBeNull();
  });

  it('removes incomplete personal orders before notifications and rendering', () => {
    const missions = createOverview('current-guild');
    missions.personalOrders.push({ canClaimReward: true } as never);

    const normalized = normalizeGuildMissionOverview(missions, 'current-guild');

    expect(normalized?.personalOrders.length).toBe(1);
    expect(normalized?.personalOrders[0].definition.name).toBe('Scout');
  });
});

describe('isHandledGuildInitiatorEcho', () => {
  it('matches only an authoritative response already handled by this actor', () => {
    expect(
      isHandledGuildInitiatorEcho(
        { actorCharacterId: 'character-1', initiatorHandled: true },
        'character-1',
      ),
    ).toBeTrue();
    expect(
      isHandledGuildInitiatorEcho(
        { actorCharacterId: 'character-2', initiatorHandled: true },
        'character-1',
      ),
    ).toBeFalse();
    expect(
      isHandledGuildInitiatorEcho(
        { actorCharacterId: 'character-1', initiatorHandled: false },
        'character-1',
      ),
    ).toBeFalse();
  });
});

describe('GuildStateService description updates', () => {
  function createState(...updateRequests: Subject<void>[]): GuildStateService {
    TestBed.configureTestingModule({});
    let updateRequestIndex = 0;

    const eventEnvelope = {
      GuildDirectoryChanged: signal(null),
      GuildInviteReceived: signal(null),
      GuildInviteRejected: signal(null),
      GuildApplicationRejected: signal(null),
      GuildMembershipChanged: signal(null),
      GuildBuildingsChanged: signal(null),
      GuildMissionsChanged: signal(null),
      GuildApplication: signal(null),
      GuildStateChanged: signal(null),
      GuildDisbanded: signal(null),
    };
    const guildService = {
      getMyGuild: jasmine.createSpy().and.returnValue(NEVER),
      updateDescription: jasmine
        .createSpy()
        .and.callFake(() =>
          updateRequests[updateRequestIndex++].asObservable(),
        ),
    };
    const eventService = {
      eventEnvelope,
      reconnectCount: signal(0),
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    const auth = {
      isAuthenticated: jasmine.createSpy().and.returnValue(false),
      currentCharacter: jasmine.createSpy().and.returnValue(null),
    };
    const notifications = {
      count: jasmine.createSpy().and.returnValue(0),
    };
    const stateSync = {
      register: jasmine.createSpy(),
      activate: jasmine.createSpy(),
    };
    return TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          auth as never,
          notifications as never,
          {} as never,
          stateSync as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );
  }

  it('shows a new description before the request completes', () => {
    const request = new Subject<void>();
    const state = createState(request);
    state.setGuild(createGuild('Old description'));

    state.updateDescription('New description');

    expect(state.guild()?.description).toBe('New description');
  });

  it('keeps the optimistic description after a successful request', () => {
    const request = new Subject<void>();
    const state = createState(request);
    state.setGuild(createGuild('Old description'));

    state.updateDescription('New description');
    request.next();

    expect(state.guild()?.description).toBe('New description');
  });

  it('restores the previous description when the request fails', () => {
    const request = new Subject<void>();
    const state = createState(request);
    state.setGuild(createGuild('Old description'));

    state.updateDescription('New description');
    request.error(new Error('Save failed'));

    expect(state.guild()?.description).toBe('Old description');
    expect(state.error()).toBe('Save failed');
  });

  it('does not let an older failure undo a newer description', () => {
    const firstRequest = new Subject<void>();
    const secondRequest = new Subject<void>();
    const state = createState(firstRequest, secondRequest);
    state.setGuild(createGuild('Old description'));

    state.updateDescription('First description');
    state.updateDescription('Second description');
    firstRequest.error(new Error('First save failed'));

    expect(state.guild()?.description).toBe('Second description');
    expect(state.error()).toBeNull();
  });
});

describe('GuildStateService refreshes', () => {
  it('does not manually reload guild or inventory after a vault mutation', () => {
    TestBed.configureTestingModule({});
    const guildService = {
      getMyGuild: jasmine.createSpy().and.returnValue(NEVER),
      donateVaultItem: jasmine.createSpy().and.returnValue(of(undefined)),
    };
    const eventService = {
      eventEnvelope: {
        GuildDirectoryChanged: signal(null),
        GuildInviteReceived: signal(null),
        GuildInviteRejected: signal(null),
        GuildApplicationRejected: signal(null),
        GuildMembershipChanged: signal(null),
        GuildBuildingsChanged: signal(null),
        GuildMissionsChanged: signal(null),
        GuildApplication: signal(null),
        GuildStateChanged: signal(null),
        GuildDisbanded: signal(null),
      },
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    const inventory = { load: jasmine.createSpy('load') };
    const state = TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          {
            isAuthenticated: () => false,
            currentCharacter: () => null,
          } as never,
          { count: () => 0 } as never,
          inventory as never,
          {
            register: jasmine.createSpy(),
            activate: jasmine.createSpy(),
          } as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );

    state.donateVaultItem('equipment-id').subscribe();

    expect(guildService.donateVaultItem).toHaveBeenCalledOnceWith(
      'equipment-id',
    );
    expect(guildService.getMyGuild).toHaveBeenCalledTimes(1);
    expect(inventory.load).not.toHaveBeenCalled();
  });

  it('shares overlapping guild refreshes', () => {
    TestBed.configureTestingModule({});
    const guildRequest = new Subject<Guild | null>();
    const guildService = {
      getMyGuild: jasmine
        .createSpy()
        .and.returnValue(guildRequest.asObservable()),
    };
    const eventService = {
      eventEnvelope: {
        GuildDirectoryChanged: signal(null),
        GuildInviteReceived: signal(null),
        GuildInviteRejected: signal(null),
        GuildApplicationRejected: signal(null),
        GuildMembershipChanged: signal(null),
        GuildBuildingsChanged: signal(null),
        GuildMissionsChanged: signal(null),
        GuildApplication: signal(null),
        GuildStateChanged: signal(null),
        GuildDisbanded: signal(null),
      },
      reconnectCount: signal(0),
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    const stateSync = {
      register: jasmine.createSpy(),
      activate: jasmine.createSpy(),
    };
    const state = TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          {
            isAuthenticated: () => false,
            currentCharacter: () => null,
          } as never,
          { count: () => 0 } as never,
          {} as never,
          stateSync as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );

    state.refresh();
    state.refresh();

    expect(guildService.getMyGuild).toHaveBeenCalledTimes(1);

    guildRequest.complete();
    state.refresh();

    expect(guildService.getMyGuild).toHaveBeenCalledTimes(2);
  });

  it('does not cascade a same-guild core refresh into every guild subresource', () => {
    TestBed.configureTestingModule({});
    const firstGuildRequest = new Subject<Guild | null>();
    const secondGuildRequest = new Subject<Guild | null>();
    let requestIndex = 0;
    const guildService = {
      getMyGuild: jasmine
        .createSpy()
        .and.callFake(() =>
          [firstGuildRequest, secondGuildRequest][
            requestIndex++
          ].asObservable(),
        ),
      getBuildings: jasmine.createSpy().and.returnValue(of(null)),
      getMissions: jasmine.createSpy().and.returnValue(of(null)),
      getShop: jasmine.createSpy().and.returnValue(of(null)),
      getAllGuilds: jasmine.createSpy().and.returnValue(of([])),
    };
    const eventService = {
      eventEnvelope: {
        GuildDirectoryChanged: signal(null),
        GuildInviteReceived: signal(null),
        GuildInviteRejected: signal(null),
        GuildApplicationRejected: signal(null),
        GuildMembershipChanged: signal(null),
        GuildBuildingsChanged: signal(null),
        GuildMissionsChanged: signal(null),
        GuildApplication: signal(null),
        GuildStateChanged: signal(null),
        GuildDisbanded: signal(null),
      },
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    const stateSync = {
      register: jasmine.createSpy(),
      activate: jasmine.createSpy(),
      resetScope: jasmine.createSpy(),
      reconcile: jasmine.createSpy().and.returnValue(Promise.resolve()),
    };
    const state = TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          {
            isAuthenticated: () => false,
            currentCharacter: () => null,
          } as never,
          {
            count: () => 0,
            initializeCount: jasmine.createSpy(),
          } as never,
          {} as never,
          stateSync as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );

    const guild = createGuild('Description');
    firstGuildRequest.next(guild);
    firstGuildRequest.complete();

    expect(guildService.getBuildings).toHaveBeenCalledTimes(1);
    expect(guildService.getMissions).toHaveBeenCalledTimes(1);
    expect(guildService.getShop).toHaveBeenCalledTimes(1);

    state.refresh();
    secondGuildRequest.next(guild);
    secondGuildRequest.complete();

    expect(guildService.getBuildings).toHaveBeenCalledTimes(1);
    expect(guildService.getMissions).toHaveBeenCalledTimes(1);
    expect(guildService.getShop).toHaveBeenCalledTimes(1);
  });

  it('does not reload the shop when guild mission progress changes', () => {
    TestBed.configureTestingModule({});
    const missionEvent = signal<{
      payload: { guildId: string };
      updateId: string;
    } | null>(null);
    const guildService = {
      getMyGuild: jasmine
        .createSpy()
        .and.returnValue(of(createGuild('Description'))),
      getBuildings: jasmine.createSpy().and.returnValue(of(null)),
      getMissions: jasmine.createSpy().and.returnValue(of(null)),
      getShop: jasmine.createSpy().and.returnValue(of(null)),
      getAllGuilds: jasmine.createSpy().and.returnValue(of([])),
    };
    const eventService = {
      eventEnvelope: {
        GuildDirectoryChanged: signal(null),
        GuildInviteReceived: signal(null),
        GuildInviteRejected: signal(null),
        GuildApplicationRejected: signal(null),
        GuildMembershipChanged: signal(null),
        GuildBuildingsChanged: signal(null),
        GuildMissionsChanged: missionEvent,
        GuildApplication: signal(null),
        GuildStateChanged: signal(null),
        GuildDisbanded: signal(null),
      },
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          {
            isAuthenticated: () => false,
            currentCharacter: () => null,
          } as never,
          {
            count: () => 0,
            initializeCount: jasmine.createSpy(),
          } as never,
          {} as never,
          {
            register: jasmine.createSpy(),
            activate: jasmine.createSpy(),
            resetScope: jasmine.createSpy(),
            reconcile: jasmine.createSpy().and.returnValue(Promise.resolve()),
          } as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );
    TestBed.flushEffects();
    expect(guildService.getShop).toHaveBeenCalledTimes(1);

    missionEvent.set({
      payload: { guildId: 'guild-id' },
      updateId: 'mission-progress-1',
    });
    TestBed.flushEffects();

    expect(guildService.getShop).toHaveBeenCalledTimes(1);
  });

  it('starts a newer guild request for a coordinator revision', () => {
    TestBed.configureTestingModule({});
    const oldRequest = new Subject<Guild | null>();
    const currentRequest = new Subject<Guild | null>();
    const guildService = {
      getMyGuild: jasmine
        .createSpy()
        .and.returnValues(
          oldRequest.asObservable(),
          currentRequest.asObservable(),
        ),
      getBuildings: jasmine.createSpy().and.returnValue(of(null)),
      getMissions: jasmine.createSpy().and.returnValue(of(null)),
      getShop: jasmine.createSpy().and.returnValue(of(null)),
      getAllGuilds: jasmine.createSpy().and.returnValue(of([])),
    };
    const eventService = {
      eventEnvelope: {
        GuildDirectoryChanged: signal(null),
        GuildInviteReceived: signal(null),
        GuildInviteRejected: signal(null),
        GuildApplicationRejected: signal(null),
        GuildMembershipChanged: signal(null),
        GuildBuildingsChanged: signal(null),
        GuildMissionsChanged: signal(null),
        GuildApplication: signal(null),
        GuildStateChanged: signal(null),
        GuildDisbanded: signal(null),
      },
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    let guildRefresh!: StateSyncRefresh;
    const stateSync = {
      register: jasmine
        .createSpy()
        .and.callFake(
          (scope: string, key: string, refresh: StateSyncRefresh) => {
            if (scope === 'guild' && key === 'guild') guildRefresh = refresh;
          },
        ),
      activate: jasmine.createSpy(),
      resetScope: jasmine.createSpy(),
      reconcile: jasmine.createSpy().and.returnValue(Promise.resolve()),
    };
    const state = TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          eventService as never,
          {
            isAuthenticated: () => false,
            currentCharacter: () => null,
          } as never,
          { count: () => 0, initializeCount: () => undefined } as never,
          {} as never,
          stateSync as never,
          TestBed.inject(DomainVersionTracker),
        ),
    );

    expect(guildService.getMyGuild).toHaveBeenCalledTimes(1);
    const refreshResult = guildRefresh({
      scope: 'guild',
      key: 'guild',
      targetRevision: 7,
    });
    (refreshResult as Observable<unknown>).subscribe();
    expect(guildService.getMyGuild).toHaveBeenCalledTimes(2);

    currentRequest.next(createGuild('Current'));
    currentRequest.complete();
    oldRequest.next(createGuild('Stale'));
    oldRequest.complete();

    expect(state.guild()?.description).toBe('Current');
  });
});

function createGuild(description: string): Guild {
  return {
    id: 'guild-id',
    name: 'Guild',
    tag: 'TAG',
    description,
    guildXp: 0,
    guildLevel: 1,
    members: [],
    maxMembers: 10,
    invites: [],
    resources: [],
    rolePermissions: [],
    vaultItems: [],
  };
}

function createOverview(guildId: string): GuildMissionOverview {
  return {
    guildId,
    guildXp: 0,
    guildLevel: 1,
    nextDailyResetAt: '2026-08-11T00:00:00Z',
    nextWeeklyResetAt: '2026-08-17T00:00:00Z',
    canSelectMission: false,
    weeklyOptions: [],
    activeMission: null,
    myWeeklyContribution: null,
    personalOrders: [
      {
        id: 'valid-order',
        definition: {
          id: 'scout',
          key: 'scout',
          name: 'Scout',
          description: 'Scout five rooms.',
          category: 'Dungeon',
          metric: 'DungeonRoomsCleared',
          baseTarget: 5,
        },
        periodKey: '2026-08-10',
        targetAmount: 5,
        currentAmount: 0,
        status: 'Active',
        canClaimReward: false,
        reward: { guildFavor: 50, guildXp: 20, guildSupplies: 10 },
        generatedAt: '2026-08-10T00:00:00Z',
      },
    ],
    contributionSummary: {
      dailyPeriodKey: '2026-08-10',
      weeklyPeriodKey: '2026-W33',
      dailyContributionScore: 0,
      weeklyContributionScore: 0,
      guildFavorEarned: 0,
      guildXpGenerated: 0,
      guildSuppliesGenerated: 0,
      ordersCompleted: 0,
    },
    contributionLeaderboard: [],
  };
}
