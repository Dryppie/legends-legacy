import {
  fakeAsync,
  flushMicrotasks,
  TestBed,
  tick,
} from '@angular/core/testing';
import { WritableSignal, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { of } from 'rxjs';
import {
  RaidLane,
  RaidPlaybackBundle,
  RaidRun,
  RaidService,
} from '../../../../core/services/api/raid/raid.service';
import { CombatService } from '../../../../core/services/client-side/combat/combat.service';
import { RaidPlaybackService } from '../../../../core/services/client-side/combat/raid-playback.service';
import { CombatStateService } from '../../../../core/state/combat-state/combat-state.service';
import { GameEventService } from '../../../../core/services/real-time/game-event.service';
import { BattleOutcome } from '../../../../shared/models/Dtos/combatResultDto';
import { RaidPageComponent } from './raid-page.component';

describe('RaidPageComponent playback', () => {
  let component: RaidPageComponent;
  let raids: jasmine.SpyObj<RaidService>;
  let combat: jasmine.SpyObj<CombatService>;
  let raidUpdated: WritableSignal<any>;

  beforeEach(() => {
    raids = jasmine.createSpyObj<RaidService>('RaidService', [
      'getRaid',
      'getPlaybackBundle',
    ]);
    raids.getRaid.and.returnValue(of(raid('Settled')));
    raids.getPlaybackBundle.and.returnValue(of(playbackBundle()));
    combat = jasmine.createSpyObj<CombatService>('CombatService', [
      'applyRaidCombatFrame',
      'closeCurrentRaidBattle',
    ]);
    raidUpdated = signal<any>(null);
    const reconnectCount = signal(0);

    TestBed.configureTestingModule({
      providers: [
        { provide: RaidService, useValue: raids },
        { provide: CombatService, useValue: combat },
        { provide: RaidPlaybackService, useValue: new RaidPlaybackService() },
        { provide: CombatStateService, useValue: {} },
        { provide: ActivatedRoute, useValue: {} },
        { provide: Router, useValue: {} },
        {
          provide: GameEventService,
          useValue: {
            eventEnvelope: { RaidUpdated: raidUpdated },
            reconnectCount,
          },
        },
      ],
    });
    component = TestBed.runInInjectionContext(() => new RaidPageComponent());
    (component as unknown as { raidRunId: string }).raidRunId = 'raid-run';
    TestBed.flushEffects();
  });

  afterEach(() => component.ngOnDestroy());

  it('resumes Flank, Ward, then Vanguard when refreshed during playback', fakeAsync(() => {
    const monotonicOrigin = Date.now();
    spyOn(performance, 'now').and.callFake(() => Date.now() - monotonicOrigin);
    const playback = raid('Playback');

    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      playback,
    );
    flushMicrotasks();

    expect(requestedLanes()).toEqual(['Flank']);
    tick(1500);
    expect(requestedLanes()).toEqual(['Flank', 'Ward']);
    tick(1500);
    expect(requestedLanes()).toEqual(['Flank', 'Ward', 'Vanguard']);
    tick(1500);

    expect(combat.applyRaidCombatFrame).toHaveBeenCalledTimes(3);
    expect(component.playbackLane()).toBeNull();
  }));

  it('skips a lane whose shared playback window already ended before refresh', fakeAsync(() => {
    const playback = raid('Playback');
    playback.playbackStartedAt = new Date(
      Date.parse(playback.serverNow) - 1600,
    ).toISOString();

    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      playback,
    );
    flushMicrotasks();

    expect(requestedLanes()).toEqual(['Flank', 'Ward']);
    expect(component.playbackLane()).toBe('Ward');
  }));

  it('collapses and expands individual raid wings', () => {
    expect(component.isLaneCollapsed('Flank')).toBeFalse();

    component.toggleLane('Flank');
    expect(component.isLaneCollapsed('Flank')).toBeTrue();
    expect(component.isLaneCollapsed('Ward')).toBeFalse();

    component.toggleLane('Flank');
    expect(component.isLaneCollapsed('Flank')).toBeFalse();
  });

  it('maps raid states to the shared Tower status badges', () => {
    expect(component.raidStatusLabel('Mustering')).toBe('Recruiting');
    expect(component.raidStatusBadge('Mustering')).toBe('Rallying');
    expect(component.raidStatusBadge('Resolving')).toBe('InProgress');
    expect(component.raidStatusBadge('Playback')).toBe('InProgress');
    expect(component.raidStatusBadge('Settled')).toBe('Succeeded');
    expect(component.raidStatusBadge('Cancelled')).toBe('Cancelled');
  });

  it('reloads when the current raid receives a committed realtime update', fakeAsync(() => {
    raids.getRaid.calls.reset();

    raidUpdated.set({
      updateId: 'raid-update-1',
      payload: { raidRunId: 'raid-run' },
    });
    TestBed.flushEffects();
    tick();

    expect(raids.getRaid).toHaveBeenCalledOnceWith('raid-run');
  }));

  function requestedLanes(): RaidLane[] {
    return raids.getPlaybackBundle.calls.allArgs().map((args) => args[1]);
  }
});

function raid(status: RaidRun['status']): RaidRun {
  return {
    id: 'raid-run',
    raidBossId: 'raid-boss.hives-abyss',
    raidBossName: "The Hive's Abyss",
    imagePath: 'ant_queen',
    region: 1,
    tier: 1,
    status,
    leaderCharacterId: 'leader',
    createdAt: '2026-08-20T00:00:00Z',
    signupClosesAt: '2026-08-20T01:00:00Z',
    commencedAt: '2026-08-20T00:00:00Z',
    playbackStartedAt:
      status === 'Playback' ? new Date(Date.now()).toISOString() : null,
    playbackEndsAt:
      status === 'Playback' ? new Date(Date.now() + 4500).toISOString() : null,
    serverNow: new Date(Date.now()).toISOString(),
    resolvedAt: status === 'Settled' ? '2026-08-20T00:01:00Z' : null,
    laneSlots: 3,
    minimumRoster: 3,
    signups: [],
    laneResults: [],
    participantResults: [],
    outcome: status === 'Settled' ? 'Slain' : null,
    reinforcementPenalty: 0,
    wardBreak: 1,
    bossHealthRemainingPercent: 0,
    canJoin: false,
    canLeave: false,
    canAssign: false,
    canCommence: false,
    canRefreshSnapshot: false,
    canClaim: status === 'Settled',
    rewardWasReduced: false,
    canPreviewBattlePlan: false,
    canCancel: false,
    canTransferLeadership: false,
    developmentToolsEnabled: true,
  };
}

function playbackBundle(): RaidPlaybackBundle {
  return {
    schemaVersion: 2,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 0,
    entities: [
      {
        index: 0,
        id: 'raider',
        name: 'Raider',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 25,
      },
      {
        index: 1,
        id: 'enemy',
        name: 'Enemy',
        imagePath: '',
        isFriendly: false,
        maxHealth: 100,
        level: 25,
      },
    ],
    abilities: [],
    frames: [
      {
        sequence: 0,
        tick: 0,
        entityStates: [
          { entityIndex: 0, health: 100, barrier: 0 },
          { entityIndex: 1, health: 0, barrier: 0 },
        ],
        entityTotals: [],
        abilityTotals: [],
        isFinal: true,
        outcome: BattleOutcome.Victory,
      },
    ],
  };
}
