import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CharacterActionDto } from '../../../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../../../shared/models/enums/characterActionType';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { GameService } from '../../client-side/game/game.service';
import { CombatService } from '../../client-side/combat/combat.service';
import { InventoryStateService } from '../inventory/inventory-state.service';
import { CharacterActionsService } from './character-actions.service';
import { CharacterActionsStateService } from './character-actions.state.service';
import { CombatActionHandler } from './handlers/combat-action-handler';
import { CraftingActionHandler } from './handlers/crafting-action-handler';
import { CharacterActionTypePersistenceService } from './helpers/character-action-type-persistence.service';
import { CharacterActionsPollingService } from './helpers/characterActionsPollingService';

describe('CharacterActionsStateService', () => {
  let service: CharacterActionsStateService;
  let actions: jasmine.SpyObj<CharacterActionsService>;
  let polling: jasmine.SpyObj<CharacterActionsPollingService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    actions = jasmine.createSpyObj<CharacterActionsService>(
      'CharacterActionsService',
      ['startCombat', 'startCrafting', 'resolveCurrentAction', 'stop'],
    );
    polling = jasmine.createSpyObj<CharacterActionsPollingService>(
      'CharacterActionsPollingService',
      ['start', 'stop'],
    );
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        CharacterActionsStateService,
        { provide: CharacterActionsService, useValue: actions },
        { provide: CharacterActionsPollingService, useValue: polling },
        {
          provide: CharacterActionTypePersistenceService,
          useValue: jasmine.createSpyObj('ActionPersistence', ['set', 'clear']),
        },
        {
          provide: CombatActionHandler,
          useValue: jasmine.createSpyObj('CombatActionHandler', ['handle']),
        },
        {
          provide: CraftingActionHandler,
          useValue: jasmine.createSpyObj('CraftingActionHandler', ['handle']),
        },
        {
          provide: GameService,
          useValue: jasmine.createSpyObj('GameService', [
            'resumeCombat',
            'endCombat',
          ]),
        },
        {
          provide: CombatService,
          useValue: jasmine.createSpyObj('CombatService', ['clearAllCombat']),
        },
        {
          provide: InventoryStateService,
          useValue: jasmine.createSpyObj('InventoryStateService', ['load']),
        },
        { provide: EventBusService, useValue: { logout: signal(0) } },
        { provide: Router, useValue: router },
      ],
    });

    service = TestBed.inject(CharacterActionsStateService);
  });

  it('stores the returned action and navigates immediately after combat starts', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(of(action));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(service.currentAction()).toBe(action);
    expect(polling.start).toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
  });

  it('recovers an already-started combat after an ambiguous start error', () => {
    const action = combatAction();
    actions.startCombat.and.returnValue(
      throwError(() => new Error('Connection closed')),
    );
    actions.resolveCurrentAction.and.returnValue(of(action));

    service.startAction(CharacterActionType.Combat, { areaId: 'lumo-ruins' });

    expect(actions.resolveCurrentAction).toHaveBeenCalled();
    expect(service.currentAction()).toBe(action);
    expect(router.navigate).toHaveBeenCalledWith(['/game/combat']);
    expect(service.idleCombatError()).toBeNull();
  });
});

function combatAction(): CharacterActionDto {
  const nextResolutionAt = new Date('2026-08-08T12:00:10Z');

  return {
    characterActionType: CharacterActionType.Combat,
    lootTableId: 'lumo-ruins',
    updatedAt: nextResolutionAt,
    nextResolutionAt,
    revision: 'combat-revision',
    isDeleted: false,
  };
}
