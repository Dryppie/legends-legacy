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

  it('resumes three preparations and the Final Assault during playback', fakeAsync(() => {
    const monotonicOrigin = Date.now();
    spyOn(performance, 'now').and.callFake(() => Date.now() - monotonicOrigin);
    const playback = raid('Playback');

    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      playback,
    );
    flushMicrotasks();

    expect(requestedLanes()).toEqual(['Rearguard', 'Vanguard', 'MainGuard']);
    expect(component.showingAllPreparations()).toBeTrue();
    expect(component.preparationViews().map((view) => view.lane)).toEqual([
      'Rearguard',
      'Vanguard',
      'MainGuard',
    ]);
    expect(combat.applyRaidCombatFrame).not.toHaveBeenCalled();

    tick(1500);
    expect(requestedLanes()).toEqual([
      'Rearguard',
      'Vanguard',
      'MainGuard',
      'FinalAssault',
    ]);
    expect(component.playbackLane()).toBe('FinalAssault');
    tick(1500);

    expect(combat.applyRaidCombatFrame).toHaveBeenCalledTimes(1);
    expect(component.playbackLane()).toBeNull();
  }));

  it('opens the Final Assault when the shared preparation window ended before refresh', fakeAsync(() => {
    const playback = raid('Playback');
    playback.playbackStartedAt = new Date(
      Date.parse(playback.serverNow) - 1600,
    ).toISOString();

    (component as unknown as { acceptRaid(value: RaidRun): void }).acceptRaid(
      playback,
    );
    flushMicrotasks();

    expect(requestedLanes()).toEqual([
      'Rearguard',
      'Vanguard',
      'MainGuard',
      'FinalAssault',
    ]);
    expect(component.playbackLane()).toBe('FinalAssault');
  }));

  it('switches between one and all preparations without hiding summaries or stopping playback', fakeAsync(() => {
    component.raid.set(raid('Settled'));

    component.replayRaid();

    expect(component.showingAllPreparations()).toBeTrue();
    component.focusPreparation('Vanguard');
    expect(component.showingAllPreparations()).toBeFalse();
    expect(component.playbackLane()).toBe('Vanguard');
    expect(component.preparationViews()).toHaveSize(3);
    expect(combat.applyRaidCombatFrame).toHaveBeenCalledTimes(1);

    component.showAllPreparations();
    expect(component.showingAllPreparations()).toBeTrue();
    expect(component.playbackLane()).toBeNull();
    expect(component.watchingPlayback()).toBeTrue();
    expect(combat.closeCurrentRaidBattle).toHaveBeenCalled();

    component.showOnePreparation();
    expect(component.showingAllPreparations()).toBeFalse();
    expect(component.playbackLane()).toBe('Vanguard');
    expect(component.preparationViews()).toHaveSize(3);
    expect(combat.applyRaidCombatFrame).toHaveBeenCalledTimes(2);
  }));

  it('locks a completed preparation summary while another party is fighting', () => {
    component.raid.set(raid('Settled'));
    component.replayRaid();
    component.focusPreparation('MainGuard');

    component.preparationViews.update((views) =>
      views.map((view) =>
        view.lane === 'Vanguard' ? { ...view, completed: false } : view,
      ),
    );

    expect(component.preparationSummaryLocked()).toBeTrue();

    component.preparationViews.update((views) =>
      views.map((view) => ({ ...view, completed: true })),
    );

    expect(component.preparationSummaryLocked()).toBeFalse();
  });

  it('maps raid states to the shared Tower status badges', () => {
    expect(component.raidStatusLabel('Mustering')).toBe('Recruiting');
    expect(component.raidStatusBadge('Mustering')).toBe('Rallying');
    expect(component.raidStatusBadge('Resolving')).toBe('InProgress');
    expect(component.raidStatusBadge('Playback')).toBe('InProgress');
    expect(component.raidStatusBadge('Settled')).toBe('Succeeded');
    expect(component.raidStatusBadge('Cancelled')).toBe('Cancelled');
  });

  it('shows the active Rearguard wave in the enemy heading', () => {
    component.playbackLane.set('Rearguard');
    component.rearguardWaveNumber.set(7);

    expect(component.raidEnemyName()).toBe('Reinforcements · Wave 7');
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
    joinRequests: [],
    laneResults: [],
    participantResults: [],
    outcome: status === 'Settled' ? 'Slain' : null,
    reinforcementPenalty: 0,
    guardianBreak: 1,
    signatureDisruption: 1,
    bossHealthRemainingPercent: 0,
    canJoin: false,
    canLeave: false,
    canAssign: false,
    canCommence: false,
    canRefreshSnapshot: false,
    canClaim: status === 'Settled',
    rewardKind: null,
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
