import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import {
  TowerPersonalExpedition,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { TowerPersonalExpeditionsComponent } from './tower-personal-expeditions.component';

describe('TowerPersonalExpeditionsComponent', () => {
  let fixture: ComponentFixture<TowerPersonalExpeditionsComponent>;
  let tower: jasmine.SpyObj<WorldTowerService>;

  beforeEach(async () => {
    tower = jasmine.createSpyObj<WorldTowerService>('WorldTowerService', [
      'getPersonalExpeditions',
    ]);
    tower.getPersonalExpeditions.and.returnValue(of([createExpedition()]));

    await TestBed.configureTestingModule({
      imports: [TowerPersonalExpeditionsComponent],
      providers: [
        provideRouter([]),
        { provide: WorldTowerService, useValue: tower },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TowerPersonalExpeditionsComponent);
    fixture.detectChanges();
  });

  it('loads and renders the current character expedition history', () => {
    expect(tower.getPersonalExpeditions).toHaveBeenCalledTimes(1);
    expect(fixture.nativeElement.textContent).toContain('Lumo Sentinel');
    expect(fixture.nativeElement.textContent).toContain('Failed');
    expect(fixture.nativeElement.textContent).toContain('Journal Keeper');
  });

  it('formats expedition metadata for the journal', () => {
    const component = fixture.componentInstance;

    expect(component.duration(125)).toBe('2:05');
    expect(component.duration(null)).toBe('—');
    expect(component.modeLabel(createExpedition())).toBe('First clear');
  });
});

function createExpedition(): TowerPersonalExpedition {
  return {
    rallyId: 'rally-id',
    attemptId: 'attempt-id',
    floorNumber: 1,
    floorName: 'The Waking Step',
    guardianName: 'Lumo Sentinel',
    mode: 'FirstClear',
    status: 'Failed',
    attemptNumber: 4,
    startedAt: '2026-08-13T11:59:23Z',
    completedAt: '2026-08-13T12:00:00Z',
    fightDurationSeconds: 37,
    participants: [
      {
        characterId: 'character-id',
        characterName: 'Journal Keeper',
        guildName: null,
        powerRating: 125,
      },
    ],
  };
}
