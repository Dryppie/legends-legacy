import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import {
  TowerHallOfFameEntry,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { CombatService } from '../../../../../core/services/client-side/combat/combat.service';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { TowerHallOfFameComponent } from './tower-hall-of-fame.component';

describe('TowerHallOfFameComponent', () => {
  let fixture: ComponentFixture<TowerHallOfFameComponent>;
  let tower: jasmine.SpyObj<WorldTowerService>;
  let combat: jasmine.SpyObj<CombatService>;

  beforeEach(async () => {
    tower = jasmine.createSpyObj<WorldTowerService>('WorldTowerService', [
      'getHallOfFame',
      'getAttemptCombatResult',
    ]);
    combat = jasmine.createSpyObj<CombatService>('CombatService', [
      'startTowerBattleSummary',
      'closeCurrentTowerBattle',
    ]);
    tower.getHallOfFame.and.returnValue(of([firstClearRecord]));
    tower.getAttemptCombatResult.and.returnValue(of({} as CombatResultDto));

    await TestBed.configureTestingModule({
      imports: [TowerHallOfFameComponent],
      providers: [
        provideRouter([]),
        { provide: WorldTowerService, useValue: tower },
        { provide: CombatService, useValue: combat },
        {
          provide: CombatStateService,
          useValue: { getIsCombatActive: () => signal(false) },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TowerHallOfFameComponent);
    fixture.detectChanges();
  });

  it('offers and starts a replay for a first-clear record', () => {
    const replayButton = fixture.nativeElement.querySelector(
      'button.history-link',
    ) as HTMLButtonElement;

    expect(replayButton.textContent).toContain('Replay');
    replayButton.click();

    expect(tower.getAttemptCombatResult).toHaveBeenCalledOnceWith(
      firstClearRecord.attemptId,
    );
    expect(combat.startTowerBattleSummary).toHaveBeenCalledTimes(1);
  });
});

const firstClearRecord: TowerHallOfFameEntry = {
  floorNumber: 1,
  floorName: 'The Waking Step',
  guardianName: 'Lumo Sentinel',
  attemptId: 'attempt-id',
  clearedAt: '2026-08-13T12:00:00Z',
  attemptNumber: 4,
  fightDurationSeconds: 37,
  participants: [],
};
