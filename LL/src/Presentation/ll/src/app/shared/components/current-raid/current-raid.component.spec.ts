import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  RaidRun,
  RaidService,
} from '../../../core/services/api/raid/raid.service';
import { CurrentRaidComponent } from './current-raid.component';

describe('CurrentRaidComponent', () => {
  let fixture: ComponentFixture<CurrentRaidComponent>;
  const activeRaid = signal<RaidRun | null>(null);

  beforeEach(async () => {
    activeRaid.set(null);
    await TestBed.configureTestingModule({
      imports: [CurrentRaidComponent],
      providers: [
        provideRouter([]),
        {
          provide: RaidService,
          useValue: {
            activeRaid: activeRaid.asReadonly(),
          },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CurrentRaidComponent);
  });

  it('links a pending raid request directly to its raid page', () => {
    activeRaid.set(
      createRaid({
        joinRequests: [{ isCurrentCharacter: true }] as RaidRun['joinRequests'],
      }),
    );

    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(link.getAttribute('href')).toBe('/game/world/raid/raid-id');
    expect(link.textContent).toContain('Raid Request');
    expect(link.textContent).toContain("The Hive's Abyss");
    expect(link.textContent).toContain('Awaiting approval');
  });

  it('describes a newly created raid as recruiting', () => {
    activeRaid.set(
      createRaid({
        signups: [{ isCurrentCharacter: true }] as RaidRun['signups'],
      }),
    );

    fixture.detectChanges();

    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    expect(link.textContent).toContain('In Raid');
    expect(link.textContent).toContain('Recruiting');
    expect(link.textContent).not.toContain('raiders');
  });

  it('stays hidden when the character has no active raid', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('a')).toBeNull();
  });
});

function createRaid(overrides: Partial<RaidRun> = {}): RaidRun {
  return {
    id: 'raid-id',
    raidBossName: "The Hive's Abyss",
    status: 'Mustering',
    minimumRoster: 9,
    signups: [{}, {}] as RaidRun['signups'],
    joinRequests: [],
    ...overrides,
  } as RaidRun;
}
