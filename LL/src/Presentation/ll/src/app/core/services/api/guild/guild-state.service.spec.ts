import { Injector, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NEVER, Subject } from 'rxjs';
import { Guild } from '../../../../shared/models/Dtos/guild/guild';
import { GuildMissionOverview } from '../../../../shared/models/Dtos/guild/guildMission';
import {
  GuildStateService,
  normalizeGuild,
  normalizeGuildMissionOverview,
} from './guild-state.service';

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

describe('GuildStateService description updates', () => {
  function createState(...updateRequests: Subject<void>[]): GuildStateService {
    TestBed.configureTestingModule({});
    let updateRequestIndex = 0;

    const eventEnvelope = {
      GuildDirectoryChangedMsg: signal(null),
      GuildInviteReceivedMsg: signal(null),
      GuildInviteRejectedMsg: signal(null),
      GuildApplicationRejectedMsg: signal(null),
      GuildMembershipChangedMsg: signal(null),
      GuildBuildingsChangedMsg: signal(null),
      GuildMissionsChangedMsg: signal(null),
      GuildApplicationMsg: signal(null),
      GuildStateChangedMsg: signal(null),
      GuildDisbandedMsg: signal(null),
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
    };
    const notifications = {
      count: jasmine.createSpy().and.returnValue(0),
    };
    const stateSync = {
      register: jasmine.createSpy(),
    };
    const injector = TestBed.inject(Injector);

    return TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          auth as never,
          notifications as never,
          {} as never,
          injector,
          stateSync as never,
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
        GuildDirectoryChangedMsg: signal(null),
        GuildInviteReceivedMsg: signal(null),
        GuildInviteRejectedMsg: signal(null),
        GuildApplicationRejectedMsg: signal(null),
        GuildMembershipChangedMsg: signal(null),
        GuildBuildingsChangedMsg: signal(null),
        GuildMissionsChangedMsg: signal(null),
        GuildApplicationMsg: signal(null),
        GuildStateChangedMsg: signal(null),
        GuildDisbandedMsg: signal(null),
      },
      reconnectCount: signal(0),
      setGuildSubscription: jasmine
        .createSpy()
        .and.returnValue(Promise.resolve()),
    };
    const stateSync = { register: jasmine.createSpy() };
    const injector = TestBed.inject(Injector);
    const state = TestBed.runInInjectionContext(
      () =>
        new GuildStateService(
          guildService as never,
          eventService as never,
          { isAuthenticated: () => false } as never,
          { count: () => 0 } as never,
          {} as never,
          injector,
          stateSync as never,
        ),
    );

    state.refresh();
    state.refresh();

    expect(guildService.getMyGuild).toHaveBeenCalledTimes(1);

    guildRequest.complete();
    state.refresh();

    expect(guildService.getMyGuild).toHaveBeenCalledTimes(2);
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
