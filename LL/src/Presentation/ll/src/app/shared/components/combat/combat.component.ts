import { Component, Input, OnDestroy, OnInit } from '@angular/core';
import { DefaultHeaderComponent } from '../default-header/default-header.component';
import { CombatAvatarComponent } from './combat-avatar/combat-avatar.component';
import { CombatOverviewComponent } from './combat-overview/combat-overview.component';
import { CombatService } from '../../../core/services/combat/combat.service';
import { CombatEvent, EventType } from '../../models/Dtos/combatEventDto';
import { NgFor, NgIf, NgStyle } from '@angular/common';
import {
  SimpleCombatEntityDto,
  CombatResultDto,
} from '../../models/Dtos/combatResultDto';
import { Subscription } from 'rxjs';
import { CountdownComponent } from '../countdown/countdown.component';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CombatCountdownComponent } from './combat-countdown/combat-countdown.component';
import { LevelingService } from '../../../core/services/leveling/leveling.service';

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
  ],
  templateUrl: './combat.component.html',
  styleUrl: './combat.component.css',
})
export class CombatComponent implements OnInit, OnDestroy {
  @Input() combatEvents: CombatEvent[] = [];
  nextCombatIn: Date | null = null;
  StopCombatButtonText: string = 'Stop Combat';

  playerCharacters: SimpleCombatEntityDto[] = [];
  enemyCharacters: SimpleCombatEntityDto[] = [];
  experienceGained: number = 0;
  subscriptions: Subscription = new Subscription();

  isCombatActive = false;
  displayCombat = false;
  isDataFetched = false;
  isLoading = false;
  isStoppingCombat = false;

  constructor(
    private combatService: CombatService,
    private characterActionService: CharacterActionsService,
    private levelingService: LevelingService,
  ) {}

  ngOnInit(): void {
    const combatIsLoadingSub =
      this.characterActionService.loadingStartCombat$.subscribe((isLoading) => {
        this.isLoading = isLoading;
      });
    this.subscriptions.add(combatIsLoadingSub);

    const nextCombatSub = this.combatService.nextCombat$.subscribe((time) => {
      if (time) {
        this.nextCombatIn = time;
        this.displayCombat = false;
        this.isLoading = false;
      }
    });
    this.subscriptions.add(nextCombatSub);

    const combatResultSub = this.combatService.combatResult$.subscribe(
      (combatResult) => {
        if (combatResult) {
          this.experienceGained = combatResult.experienceGained;
          this.isDataFetched = true;
          if (!this.isLoading) {
            this.startActualCombat(combatResult);
          } else {
            this.handleCombatSetup(combatResult);
          }
          this.displayCombat = true;
          this.isLoading = false;
        }
      },
    );
    this.subscriptions.add(combatResultSub);

    const combatEventsSub = this.combatService.combatEvents$.subscribe(
      (event) => {
        this.handleCombatEvent(event);
      },
    );
    this.subscriptions.add(combatEventsSub);

    const combatOutcomeSub = this.combatService.combatOutcome$.subscribe(
      (outcome) => {
        if (outcome !== null) {
          // Add some kind of animation during this this. Then after one second, reset the teams and empty combat events
          this.levelingService.gainExperience(this.experienceGained);
          if (this.isStoppingCombat) {
            this.stopCombat();
            this.combatEnded();
          } else {
            setTimeout(() => {
              this.combatEnded();
            }, 1000);
          }
        }
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

  onCountdownComplete() {
    if (
      this.isDataFetched &&
      this.playerCharacters.length > 0 &&
      this.enemyCharacters.length > 0
    ) {
      this.startActualCombat();
      this.isLoading = false;
      console.log('starting actual combat after countdown completed');
    }
  }

  private startActualCombat(combatResult?: CombatResultDto) {
    if (combatResult) {
      this.handleCombatSetup(combatResult);
    }
    this.displayCombat = true;
  }

  initiateStoppingCombat() {
    this.isStoppingCombat = true;
    this.StopCombatButtonText = 'Stopping combat..';
    this.characterActionService.stopCharacterAction();
  }

  stopCombat() {
    this.combatEnded();
    this.subscriptions.unsubscribe();
    this.combatService.clearCurrentCombat();
  }

  combatEnded() {
    this.combatEvents = [];
    this.resetTeams();
  }

  handleCombatSetup(combatResult: CombatResultDto | null) {
    if (!combatResult) return;
    this.playerCharacters = combatResult.playerTeam;
    this.enemyCharacters = combatResult.enemyTeam;
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

    // Always push to the events list so the UI can display it
    this.combatEvents.push(event);
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

  private resetTeams(): void {
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
