import { signal } from '@angular/core';
import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { Router } from '@angular/router';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../../../core/services/api/game-bootstrap/game-bootstrap-state.service';
import { EquipmentStateService } from '../../../core/services/api/equipment/equipment-state.service';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { RegionService } from '../../../core/services/client-side/region/region.service';
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
  let refreshCurrentAction: jasmine.Spy;
  let combatResult: ReturnType<typeof signal<any>>;
  let combatOutcome: ReturnType<typeof signal<BattleOutcome | null>>;
  let combatActive: ReturnType<typeof signal<boolean>>;

  beforeEach(async () => {
    currentAction = signal<Record<string, unknown> | null>({
      characterActionType: CharacterActionType.Combat,
      isDeleted: false,
    });
    bootstrapLoaded = signal(true);
    combatResult = signal(null);
    combatOutcome = signal<BattleOutcome | null>(null);
    combatActive = signal(false);
    refreshCurrentAction = jasmine.createSpy('refreshCurrentAction');
    router = {
      url: '/game/combat',
      navigate: jasmine.createSpy('navigate').and.resolveTo(true),
    };
    const characterActions = {
      currentAction,
      loadingCombat: signal(false),
      loadingActionRefresh: signal(false),
      resolvingOfflineProgress: signal(false),
      idleCombatError: signal(null),
      stopAction: jasmine.createSpy('stopAction'),
      clear: jasmine.createSpy('clear'),
      refreshCurrentAction,
      retryIdleCombatResolution: jasmine.createSpy('retryIdleCombatResolution'),
    };
    const combatState = {
      getCombatResult: () => combatResult,
      getIsCombatActive: () => combatActive,
      getPlayerCharacters: () => signal([]),
      getEnemyCharacters: () => signal([]),
      getEntityStats: () => signal([]),
      getNextCombat: () => signal(new Date()),
      getCombatOutcome: () => combatOutcome,
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
          provide: EquipmentStateService,
          useValue: { getSlot: jasmine.createSpy('getSlot') },
        },
        {
          provide: CharacterStateService,
          useValue: { currentCharacterId: signal('current-character') },
        },
        {
          provide: GameService,
          useValue: { endCombat: jasmine.createSpy('endCombat') },
        },
        {
          provide: RegionService,
          useValue: {
            getRegionNameByAreaId: (areaId: string) =>
              areaId === 'region_02_area_02' ? 'Meran' : null,
          },
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

  it('keeps a disabled combat summary open and shows its override label', () => {
    const component = fixture.componentInstance;
    const emitSpy = spyOn(component.skipBattle, 'emit');
    component.battleType = BattleType.Raid;
    combatActive.set(true);
    combatOutcome.set(BattleOutcome.Victory);
    component.combatActionDisabled = true;
    component.combatActionButtonTextOverride = 'Waiting for parties';

    component.onStopOrSkip();
    component.onEscapeKey();
    fixture.detectChanges();

    const actionButton = fixture.nativeElement.querySelector(
      'app-mini-button button',
    ) as HTMLButtonElement;

    expect(component.combatActionButtonText()).toBe('Waiting for parties');
    expect(component.combatActionButtonMobileText()).toBe(
      'Waiting for parties',
    );
    expect(actionButton.disabled).toBeTrue();
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('removes nested vertical scrolling when the parent owns scrolling', () => {
    const component = fixture.componentInstance;
    component.useParentScroll = true;
    combatActive.set(true);

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.overflow-y-auto')).toBeNull();
  });

  it('uses the active area parent region in the idle combat title', () => {
    currentAction.set({
      characterActionType: CharacterActionType.Combat,
      isDeleted: false,
      combatActionDetails: {
        area: {
          id: 'region_02_area_02',
          name: 'Rotgrave Fields',
        },
      },
    });

    expect(fixture.componentInstance.battleTitle()).toBe(
      'Meran — Rotgrave Fields',
    );
  });

  it('does not clear a successor action when delayed Combat cleanup runs', () => {
    const characterActions = TestBed.inject(
      CharacterActionsStateService,
    ) as any;

    fixture.componentInstance.battleType = BattleType.IdleCombat;
    fixture.componentInstance.stopCombat();

    expect(characterActions.clear).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledOnceWith(['/game/world']);
  });

  it('hides a cached idle-combat summary after the action switches to Idle', async () => {
    combatResult.set({
      playerTeam: [{ id: 'player' }],
      enemyTeam: [{ id: 'enemy' }],
      entityStats: [],
      duration: 10,
    });
    fixture.detectChanges();
    expect(fixture.componentInstance.displayCombat).toBeTrue();

    currentAction.set({
      characterActionType: CharacterActionType.Idle,
      isDeleted: false,
    });
    fixture.detectChanges();
    await Promise.resolve();
    fixture.detectChanges();

    expect(fixture.componentInstance.displayCombat).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('No Active Combat');
    expect(refreshCurrentAction).not.toHaveBeenCalled();
  });

  it('cancels delayed Combat exit after the player navigates away', fakeAsync(() => {
    fixture.detectChanges();
    fixture.componentInstance.initiateStoppingCombat();
    combatOutcome.set(BattleOutcome.Victory);
    tick();

    fixture.destroy();
    tick(3_001);

    expect(router.navigate).not.toHaveBeenCalled();
  }));
});
