import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CombatStateService } from '../../../core/state/combat-state/combat-state.service';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { CombatComponent } from './combat.component';

describe('CombatComponent', () => {
  let fixture: ComponentFixture<CombatComponent>;
  let currentAction: ReturnType<typeof signal<Record<string, unknown> | null>>;
  let router: { url: string; navigate: jasmine.Spy };

  beforeEach(async () => {
    currentAction = signal<Record<string, unknown> | null>({
      characterActionType: CharacterActionType.Combat,
      isDeleted: false,
    });
    router = {
      url: '/game/combat',
      navigate: jasmine.createSpy('navigate').and.resolveTo(true),
    };

    const characterActions = {
      currentAction,
      loadingCombat: signal(false),
      loadingActionRefresh: signal(false),
      idleCombatError: signal(null),
      stopAction: jasmine.createSpy('stopAction'),
      clear: jasmine.createSpy('clear'),
      retryIdleCombatResolution: jasmine.createSpy('retryIdleCombatResolution'),
    };
    const combatState = {
      getCombatResult: () => signal(null),
      getIsCombatActive: () => signal(false),
      getPlayerCharacters: () => signal([]),
      getEnemyCharacters: () => signal([]),
      getEntityStats: () => signal([]),
      getNextCombat: () => signal(new Date()),
      getCombatOutcome: () => signal(null),
    };

    await TestBed.configureTestingModule({
      imports: [CombatComponent],
      providers: [
        { provide: CharacterActionsStateService, useValue: characterActions },
        { provide: CombatStateService, useValue: combatState },
        {
          provide: GameService,
          useValue: { endCombat: jasmine.createSpy('endCombat') },
        },
        {
          provide: FirstPartyTourService,
          useValue: { start: jasmine.createSpy('start') },
        },
        { provide: Router, useValue: router },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CombatComponent);
  });

  it('does not show a guide launcher in the pending idle-combat card', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Combat Is Ongoing');
    expect(fixture.nativeElement.querySelector('app-help-launcher')).toBeNull();
  });

  it('returns to the world when the idle-combat action is stopped', async () => {
    fixture.detectChanges();
    expect(router.navigate).not.toHaveBeenCalled();

    currentAction.set({
      characterActionType: CharacterActionType.Combat,
      isDeleted: true,
    });
    fixture.detectChanges();
    await Promise.resolve();

    expect(router.navigate).toHaveBeenCalledOnceWith(['/game/world']);
  });
});
