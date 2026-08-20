import {
  fakeAsync,
  flushMicrotasks,
  TestBed,
  tick,
} from '@angular/core/testing';
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
import { StateSyncCoordinator } from '../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import { BattleOutcome } from '../../../../shared/models/Dtos/combatResultDto';
import { RaidPageComponent } from './raid-page.component';

describe('RaidPageComponent playback', () => {
  let component: RaidPageComponent;
  let raids: jasmine.SpyObj<RaidService>;
  let combat: jasmine.SpyObj<CombatService>;

  beforeEach(() => {
    raids = jasmine.createSpyObj<RaidService>('RaidService', [
      'getPlaybackBundle',
    ]);
    raids.getPlaybackBundle.and.returnValue(of(playbackBundle()));
    combat = jasmine.createSpyObj<CombatService>('CombatService', [
      'applyRaidCombatFrame',
      'closeCurrentRaidBattle',
    ]);

    TestBed.configureTestingModule({
      providers: [
        { provide: RaidService, useValue: raids },
        { provide: CombatService, useValue: combat },
        { provide: RaidPlaybackService, useValue: new RaidPlaybackService() },
        { provide: CombatStateService, useValue: {} },
        { provide: ActivatedRoute, useValue: {} },
        { provide: Router, useValue: {} },
        { provide: StateSyncCoordinator, useValue: {} },
      ],
    });
    component = TestBed.runInInjectionContext(() => new RaidPageComponent());
    (component as unknown as { raidRunId: string }).raidRunId = 'raid-run';
  });

  afterEach(() => component.ngOnDestroy());

  it('automatically plays Flank, Ward, then Vanguard after resolution', fakeAsync(() => {
    const resolving = raid('Resolving');
    const settled = raid('Settled');

    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      resolving,
    );
    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      settled,
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
    expect(component.raidStatusBadge('Settled')).toBe('Succeeded');
    expect(component.raidStatusBadge('Cancelled')).toBe('Cancelled');
  });

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
