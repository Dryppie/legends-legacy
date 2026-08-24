import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import {
  RaidBossSummary,
  RaidService,
} from '../../../../../core/services/api/raid/raid.service';
import { GameRealtimeEventRegistry } from '../../../../../core/services/real-time/game-realtime/game-realtime-event-registry.service';
import { RaidsComponent } from './raids.component';

describe('RaidsComponent', () => {
  let fixture: ComponentFixture<RaidsComponent>;

  beforeEach(async () => {
    const raidDirectoryUpdated = signal(null);

    await TestBed.configureTestingModule({
      imports: [RaidsComponent],
      providers: [
        provideRouter([]),
        {
          provide: RaidService,
          useValue: {
            getOpenRaids: () => of([]),
            getHistory: () => of([]),
          },
        },
        {
          provide: GameRealtimeEventRegistry,
          useValue: {
            eventEnvelope: {
              RaidDirectoryUpdated: raidDirectoryUpdated.asReadonly(),
            },
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RaidsComponent);
  });

  it('makes the active raid state explicit and links back to it', () => {
    fixture.componentRef.setInput(
      'raidBoss',
      createRaidBoss({ activeRaidId: 'active-raid-id' }),
    );
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector(
      '.active-raid-status',
    ) as HTMLElement;
    const link = status.querySelector('a') as HTMLAnchorElement;

    expect(status.textContent).toContain('You’re already in a raid');
    expect(status.textContent).toContain('Return to my raid');
    expect(link.getAttribute('href')).toBe('/game/world/raid/active-raid-id');
    expect(
      fixture.nativeElement.querySelector('.raid-actions button'),
    ).toBeNull();
  });

  it('shows the create action when there is no active raid', () => {
    fixture.componentRef.setInput('raidBoss', createRaidBoss());
    fixture.detectChanges();

    const createButton = fixture.nativeElement.querySelector(
      '.raid-actions button',
    ) as HTMLButtonElement;

    expect(
      fixture.nativeElement.querySelector('.active-raid-status'),
    ).toBeNull();
    expect(createButton.textContent).toContain('Create raid');
  });
});

function createRaidBoss(
  overrides: Partial<RaidBossSummary> = {},
): RaidBossSummary {
  return {
    id: 'raid-boss',
    name: "The Hive's Abyss",
    region: 1,
    regions: [1],
    levelRequirement: 1,
    imagePath: '',
    isUnlocked: true,
    lockReason: null,
    openRaidCount: 0,
    hasWeeklyRewardThisWeek: false,
    activeRaidId: null,
    tiers: [
      {
        tier: 0,
        laneSlots: 3,
        minimumRoster: 3,
        signupWindowHours: 8,
        recommendedWingPower: {
          rearguard: 100,
          vanguard: 100,
          mainGuard: 100,
        },
      },
    ],
    developmentToolsEnabled: false,
    ...overrides,
  };
}
