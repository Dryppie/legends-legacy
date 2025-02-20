import { Component, OnDestroy, OnInit } from '@angular/core';
import { CombatAvatarComponent } from './combat-avatar/combat-avatar.component';
import { CombatOverviewComponent } from './combat-overview/combat-overview.component';
import { CombatEvent, EventType } from '../../models/Dtos/combatEventDto';
import { AsyncPipe, NgFor, NgIf, NgStyle } from '@angular/common';
import { SimpleCombatEntityDto } from '../../models/Dtos/combatResultDto';
import { Subscription } from 'rxjs';
import { CountdownComponent } from '../countdown/countdown.component';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CombatCountdownComponent } from './combat-countdown/combat-countdown.component';
import { GameService } from '../../../core/services/game/game.service';
import { CombatStateService } from '../../../core/state/combat-state/combat-state.service';

@Component({
  selector: 'app-combat',
  standalone: true,
  imports: [
    CombatAvatarComponent,
    CombatOverviewComponent,
    NgFor,
    NgIf,
    NgStyle,
    CountdownComponent,
    CombatCountdownComponent,
    AsyncPipe,
  ],
  templateUrl: './combat.component.html',
  styleUrl: './combat.component.css',
})
export class CombatComponent implements OnInit, OnDestroy {
  combatEvents: CombatEvent[] = [];
  private lastEventsLength = 0;

  nextCombatIn: Date | null = null;
  stopCombatButtonText: string = 'Stop Combat';

  playerCharacters: SimpleCombatEntityDto[] = [];
  enemyCharacters: SimpleCombatEntityDto[] = [];
  subscriptions: Subscription = new Subscription();

  isCombatActive = false;
  displayCombat = false;
  isDataFetched = false;
  isLoading = false;
  isStoppingCombat = false;

  constructor(
    private characterActionService: CharacterActionsService,
    private gameService: GameService,
    public combatStateService: CombatStateService,
  ) {}

  ngOnInit(): void {
    const playerCharactersSub =
      this.combatStateService.playerCharacters$.subscribe(
        (entities) => (this.playerCharacters = entities),
      );
    this.subscriptions.add(playerCharactersSub);

    const enemyCharactersSub =
      this.combatStateService.enemyCharacters$.subscribe((entities) => {
        this.enemyCharacters = entities;
        console.log(entities);
      });
    this.subscriptions.add(enemyCharactersSub);

    const combatIsActiveSub = this.combatStateService.isCombatActive$.subscribe(
      (isActive) => (this.isCombatActive = isActive),
    );
    this.subscriptions.add(combatIsActiveSub);

    const combatIsLoadingSub =
      this.characterActionService.loadingStartCombat$.subscribe((isLoading) => {
        this.isLoading = isLoading;
      });
    this.subscriptions.add(combatIsLoadingSub);

    const nextCombatSub = this.combatStateService.nextCombat$.subscribe(
      (time) => {
        if (time == null) return;
        this.nextCombatIn = time;
        this.displayCombat = false;
        this.isLoading = false;
      },
    );
    this.subscriptions.add(nextCombatSub);

    const combatResultSub = this.combatStateService.combatResult$.subscribe(
      (combatResult) => {
        if (combatResult == null) return;
        this.isDataFetched = true;
        this.displayCombat = true;
      },
    );
    this.subscriptions.add(combatResultSub);

    const combatEventsSub = this.combatStateService.combatEvents$.subscribe(
      (allEvents) => {
        // 1) Identify newly arrived events by slicing from lastEventsLength
        const newEvents = allEvents.slice(this.lastEventsLength);
        this.lastEventsLength = allEvents.length;

        // 2) Handle only these new events
        newEvents.forEach((event) => {
          this.handleCombatEvent(event);
        });
      },
    );
    this.subscriptions.add(combatEventsSub);

    const combatOutcomeSub = this.combatStateService.combatOutcome$.subscribe(
      (outcome) => {
        if (outcome == null) return;

        // Add some kind of animation during this this. Then after one second, reset the teams and empty combat events
        setTimeout(() => {
          this.combatEnded();
          if (this.isStoppingCombat) {
            this.stopCombat();
          }
        }, 1000);
      },
    );
    this.subscriptions.add(combatOutcomeSub);
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  startCombat() {
    if (this.isLoading) return;
    this.isLoading = true;
  }

  initiateStoppingCombat() {
    this.isStoppingCombat = true;
    this.stopCombatButtonText = 'Stopping combat..';
    this.characterActionService.stopCharacterAction();
  }

  stopCombat() {
    this.combatEnded();
    this.subscriptions.unsubscribe();
    this.gameService.endCombat();
    this.characterActionService.clearCurrentAction();
    this.combatStateService.resetCombatState();
  }

  combatEnded() {
    this.combatStateService.resetCombatState();
    this.resetTeamSelections();
  }

  private handleCombatEvent(event: CombatEvent | null): void {
    if (!event) return;

    switch (event.eventType) {
      case EventType.AbilityUse:
        this.handleAbilityUseEvent(event);
        break;
      case EventType.Damage:
        this.handleDamageEvent(event);
        break;
      case EventType.DamageOverTime:
        this.handleDamageEvent(event);
        break;
      case EventType.DamageCrit:
        this.handleDamageEvent(event);
        break;
      case EventType.Miss:
        this.handleMissEvent(event);
        break;
      // case EventType.Parry:
      //   this.handleBlockEvent(event);
      //   break;
      case EventType.Block:
        this.handleBlockEvent(event);
        break;
      case EventType.Heal:
        this.handleHealEvent(event);
        break;
      case EventType.HealOverTime:
        this.handleHealEvent(event);
        break;
      case EventType.HealCrit:
        this.handleHealEvent(event);
        break;
      case EventType.Summon:
        this.handleSummonEvent(event);
        break;
      case EventType.SummonExpired:
        this.handleSummonExpiredEvent(event);
        break;
      case EventType.Buff:
        this.handleBuffEvent(event);
        break;
      case EventType.BuffExpired:
        const buffExpired = true;
        this.handleBuffEvent(event, buffExpired);
        break;
      case EventType.Debuff:
        this.handleDebuffEvent(event);
        break;
      case EventType.DebuffExpired:
        const debuffExpired = true;
        this.handleDebuffEvent(event, debuffExpired);
        break;
      // Add other event types as needed
      default:
        console.warn(`Unhandled event type: ${event.eventType}`);
    }
  }

  private handleAbilityUseEvent(event: CombatEvent) {
    this.updateCharacter(event.actorId, event.combatEntity);
  }

  private handleDamageEvent(event: CombatEvent): void {
    const character = this.findCharacterById(event.targetId);
    if (!character) return;
    character.health = Math.max(character.health - event.magnitude, 0);
    // console.log(`Damage: ${event.details}`);
  }

  private handleHealEvent(event: CombatEvent): void {
    const character = this.findCharacterById(event.targetId);
    if (!character) return;

    character.health = Math.min(
      character.health + event.magnitude,
      character.maxHealth,
    );
    // console.log(`Healed: ${event.details}`);
  }

  private handleMissEvent(event: CombatEvent): void {
    // Implement specific logic for handling block events
    // console.log(`Blocked: ${event.details}`);
  }

  private handleBlockEvent(event: CombatEvent): void {
    // Implement specific logic for handling block events
    // console.log(`Blocked: ${event.details}`);
  }

  private handleSummonEvent(event: CombatEvent): void {
    const summonedCharacter = event.combatEntity;
    console.log(summonedCharacter);
    if (this.isEntityInPlayerTeam(event.actorId)) {
      this.playerCharacters.push(summonedCharacter);
    } else {
      this.enemyCharacters.push(summonedCharacter);
    }
  }

  private handleSummonExpiredEvent(event: CombatEvent): void {
    if (this.isEntityInPlayerTeam(event.actorId)) {
      this.playerCharacters = this.playerCharacters.filter(
        (c) => c.id !== event.targetId,
      );
      this.focusCharacter('player', 0);
    } else {
      this.enemyCharacters = this.enemyCharacters.filter(
        (c) => c.id !== event.targetId,
      );
      this.focusCharacter('enemy', 0);
    }
    // console.log(`Summon Expired: ${event.details}`);
  }

  private handleBuffEvent(event: CombatEvent, buffExpired: boolean = false) {
    this.updateCharacter(event.targetId, event.combatEntity);
    // if (buffExpired) console.log(`Buff Expired: ${event.details}`);
    // else console.log(`Buff: ${event.details}`);
  }

  private handleDebuffEvent(event: CombatEvent, buffExpired: boolean = false) {
    // if (buffExpired) console.log(`Buff Expired: ${event.details}`);
    // else console.log(`Buff: ${event.details}`);
  }

  private updateCharacter(
    entityId: string,
    combatEntity: SimpleCombatEntityDto,
  ) {
    const character = this.findCharacterById(entityId);
    if (!character) return;
    character.health = combatEntity.health;
    character.maxHealth = combatEntity.maxHealth;
    character.mana = combatEntity.mana;
    character.maxMana = combatEntity.maxMana;
  }

  private findCharacterById(id: string): SimpleCombatEntityDto | undefined {
    return (
      this.playerCharacters.find((c) => c.id === id) ||
      this.enemyCharacters.find((c) => c.id === id)
    );
  }

  private isEntityInPlayerTeam(actorId: string): boolean {
    return this.playerCharacters.some((c) => c.id === actorId);
  }

  private resetTeamSelections(): void {
    this.selectedPlayerCharacterIndex = 0;
    this.selectedEnemyCharacterIndex = 0;
  }

  /// Selectable characters

  selectedPlayerCharacterIndex: number = 0;
  selectedEnemyCharacterIndex: number = 0;

  get selectedPlayerCharacter() {
    return this.playerCharacters[this.selectedPlayerCharacterIndex];
  }

  get selectedEnemyCharacter() {
    return this.enemyCharacters[this.selectedEnemyCharacterIndex];
  }

  focusCharacter(team: 'player' | 'enemy', index: number) {
    if (team === 'player') {
      this.selectedPlayerCharacterIndex = index;
    } else {
      this.selectedEnemyCharacterIndex = index;
    }
  }
}
