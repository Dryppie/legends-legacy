import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../../../core/services/api/game-bootstrap/game-bootstrap-state.service';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CombatStateService } from '../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../core/state/combat-state/combatState';
import { BattleOutcome } from '../../models/Dtos/combatResultDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { CombatComponent } from './combat.component';

describe('CombatComponent', () => {
  let fixture: ComponentFixture<CombatComponent>;
  let currentAction: ReturnType<typeof signal<Record<string, unknown> | null>>;
  let bootstrapLoaded: ReturnType<typeof signal<boolean>>;
  let router: { url: string; navigate: jasmine.Spy };
  let tour: { start: jasmine.Spy; stop: jasmine.Spy };
  let refreshCurrentAction: jasmine.Spy;

  beforeEach(async () => {
    currentAction = signal<Record<string, unknown> | null>({
      characterActionType: CharacterActionType.Combat,
      isDeleted: false,
    });
    bootstrapLoaded = signal(true);
    refreshCurrentAction = jasmine.createSpy('refreshCurrentAction');
    router = {
      url: '/game/combat',
      navigate: jasmine.createSpy('navigate').and.resolveTo(true),
    };
    tour = {
      start: jasmine.createSpy('start'),
      stop: jasmine.createSpy('stop'),
    };

    const characterActions = {
      currentAction,
      loadingCombat: signal(false),
      loadingActionRefresh: signal(false),
      idleCombatError: signal(null),
      stopAction: jasmine.createSpy('stopAction'),
      clear: jasmine.createSpy('clear'),
      refreshCurrentAction,
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
        {
          provide: GameBootstrapStateService,
          useValue: { loaded: bootstrapLoaded },
        },
        { provide: CombatStateService, useValue: combatState },
        {
          provide: GameService,
          useValue: { endCombat: jasmine.createSpy('endCombat') },
        },
        {
          provide: FirstPartyTourService,
          useValue: tour,
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

  it('keeps the combat route stable while a missing action is reconciled', async () => {
    fixture.detectChanges();

    currentAction.set(null);
    fixture.detectChanges();
    await Promise.resolve();

    expect(refreshCurrentAction).toHaveBeenCalledTimes(1);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('does not reconcile while a confirmed combat start is settling', async () => {
    currentAction.set(null);
    const characterActions = TestBed.inject(
      CharacterActionsStateService,
    ) as any;
    characterActions.loadingCombat.set?.(true);

    fixture.detectChanges();
    await Promise.resolve();

    expect(refreshCurrentAction).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('waits for bootstrap before reconciling a directly loaded combat route', async () => {
    currentAction.set(null);
    bootstrapLoaded.set(false);

    fixture.detectChanges();
    await Promise.resolve();

    expect(refreshCurrentAction).not.toHaveBeenCalled();
  });

  it('offers an explicit return when no server combat exists', async () => {
    currentAction.set(null);
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('No Active Combat');

    fixture.componentInstance.returnToWorld();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/game/world']);
  });

  it('does not start delayed tutorial guidance after the summary is destroyed', async () => {
    fixture.componentInstance.battleType = BattleType.Training;
    fixture.detectChanges();

    fixture.destroy();
    await new Promise((resolve) => setTimeout(resolve, 15));

    expect(tour.start).not.toHaveBeenCalled();
    expect(tour.stop).toHaveBeenCalledOnceWith(false);
  });

  it('shows the Escape shortcut on Arena and Dungeon summary buttons', () => {
    fixture.componentInstance.battleType = BattleType.Colosseum;
    expect(fixture.componentInstance.combatActionButtonText()).toBe(
      'Close Summary (Esc)',
    );
    expect(fixture.componentInstance.combatActionButtonMobileText()).toBe(
      'Close Summary',
    );

    fixture.componentInstance.battleType = BattleType.Dungeon;
    expect(fixture.componentInstance.combatActionButtonText()).toBe(
      'Close Summary (Esc)',
    );
    expect(fixture.componentInstance.combatActionButtonMobileText()).toBe(
      'Close Summary',
    );
  });

  it('closes a completed Arena or Dungeon summary when Escape is pressed', () => {
    const component = fixture.componentInstance;
    const emitSpy = spyOn(component.skipBattle, 'emit');
    component.displayCombat = true;
    component.outcome = BattleOutcome.Victory;

    component.battleType = BattleType.Colosseum;
    component.onEscapeKey();
    component.battleType = BattleType.Dungeon;
    component.onEscapeKey();

    expect(emitSpy).toHaveBeenCalledTimes(2);
  });

  it('does not close other combat views when Escape is pressed', () => {
    const component = fixture.componentInstance;
    const emitSpy = spyOn(component.skipBattle, 'emit');
    component.displayCombat = true;
    component.outcome = BattleOutcome.Victory;
    component.battleType = BattleType.Training;

    component.onEscapeKey();

    expect(emitSpy).not.toHaveBeenCalled();
  });
});
