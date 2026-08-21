import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RaidRun } from '../../../../../core/services/api/raid/raid.service';
import { RaidPartyBuilderComponent } from './raid-party-builder.component';

describe('RaidPartyBuilderComponent', () => {
  let fixture: ComponentFixture<RaidPartyBuilderComponent>;
  let component: RaidPartyBuilderComponent;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RaidPartyBuilderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RaidPartyBuilderComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('raid', raid());
    fixture.detectChanges();
  });

  it('collapses and expands individual raid wings', () => {
    expect(component.isLaneCollapsed('Rearguard')).toBeFalse();

    component.toggleLane('Rearguard');
    expect(component.isLaneCollapsed('Rearguard')).toBeTrue();
    expect(component.isLaneCollapsed('MainGuard')).toBeFalse();

    component.toggleLane('Rearguard');
    expect(component.isLaneCollapsed('Rearguard')).toBeFalse();
  });

  it('emits a complete assignment set when placing a selected raider', () => {
    const assignments: unknown[] = [];
    component.assignmentsChange.subscribe((value) => assignments.push(value));
    const current = raid();
    fixture.componentRef.setInput('raid', current);
    component.selectSignup(current.signups[0], current);

    component.placeSelectedInSlot('Vanguard', 1, current);

    expect(assignments).toEqual([
      [
        {
          characterId: 'raider-1',
          lane: 'Vanguard',
          wingSlotIndex: 1,
        },
      ],
    ]);
  });
});

function raid(): RaidRun {
  return {
    id: 'raid-run',
    raidBossId: 'raid-boss.hives-abyss',
    raidBossName: "The Hive's Abyss",
    imagePath: 'ant_queen',
    region: 1,
    tier: 1,
    status: 'Mustering',
    leaderCharacterId: 'raider-1',
    createdAt: '2026-08-20T00:00:00Z',
    signupClosesAt: '2026-08-20T01:00:00Z',
    commencedAt: null,
    playbackStartedAt: null,
    playbackEndsAt: null,
    serverNow: '2026-08-20T00:00:00Z',
    resolvedAt: null,
    laneSlots: 3,
    minimumRoster: 3,
    signups: [
      {
        characterId: 'raider-1',
        characterName: 'Raider One',
        powerRating: 100,
        lane: null,
        wingSlotIndex: null,
        signedUpAt: '2026-08-20T00:00:00Z',
        snapshotRefreshedAt: null,
        isLeader: true,
        isCurrentCharacter: true,
      },
    ],
    joinRequests: [],
    laneResults: [],
    participantResults: [],
    outcome: null,
    reinforcementPenalty: 0,
    guardianBreak: 0,
    signatureDisruption: 0,
    bossHealthRemainingPercent: 100,
    canJoin: false,
    canLeave: true,
    canAssign: true,
    canCommence: false,
    canRefreshSnapshot: true,
    canClaim: false,
    rewardKind: null,
    canPreviewBattlePlan: false,
    canCancel: true,
    canTransferLeadership: false,
    developmentToolsEnabled: true,
  };
}
