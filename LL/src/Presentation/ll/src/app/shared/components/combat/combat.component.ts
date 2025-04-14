import { Component, OnDestroy, OnInit } from '@angular/core';
import { CombatAvatarComponent } from './combat-avatar/combat-avatar.component';
import { CombatOverviewComponent } from './combat-overview/combat-overview.component';
import { CombatEvent, EventType } from '../../models/Dtos/combatEventDto';
import { AsyncPipe, NgFor, NgIf, NgStyle } from '@angular/common';
import { SimpleCombatEntityDto } from '../../models/Dtos/combatResultDto';
import { Observable, Subscription } from 'rxjs';
import { CountdownComponent } from '../countdown/countdown.component';
import { CharacterActionsService } from '../../../core/services/api/character-actions/character-actions.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CombatStateService } from '../../../core/state/combat-state/combat-state.service';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { MiniButtonComponent } from '../mini-button/mini-button.component';

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
    AsyncPipe,
    MiniButtonComponent,
  ],
  templateUrl: './combat.component.html',
  styleUrl: './combat.component.css',
})
export class CombatComponent implements OnInit, OnDestroy {
  combatEvents: CombatEvent[] = [];
  private lastEventsLength = 0;

  isStoppingCombat = false;
  nextCombatIn: Date | null = null;
  stopCombatButtonText: string = 'Stop Combat';

  playerCharacters: SimpleCombatEntityDto[] = [];
  enemyCharacters: SimpleCombatEntityDto[] = [];
  subscriptions: Subscription = new Subscription();
  currentAction$!: Observable<CharacterActionDto | null>;

  // Only set to true if a combat result has been received, or if start combat has been
  displayCombat = false;
  isCombatVisible$!: Observable<boolean>;
  isLoading = false;

  constructor(
    private characterActionService: CharacterActionsService,
    private gameService: GameService,
    public combatStateService: CombatStateService,
  ) {}

  ngOnInit(): void {
    this.currentAction$ = this.characterActionService.currentAction$;

    const isLoadingSub =
      this.characterActionService.loadingCombatAction$.subscribe(
        (isLoading) => {
          this.isLoading = isLoading;
        },
      );
    this.subscriptions.add(isLoadingSub);

    this.playerCharacters = [
      {
        name: '',
        id: '',
        imagePath: '',
        health: 100,
        maxHealth: 100,
        mana: 100,
        maxMana: 100,
        barrier: 0,
      },
    ];
    this.enemyCharacters = [
      {
        name: '',
        id: '',
        imagePath: '',
        health: 100,
        maxHealth: 100,
        mana: 100,
        maxMana: 100,
        barrier: 0,
      },
    ];
    const isCombatActiveSub = this.gameService.combatActive$.subscribe(
      (isCombatActive) => {
        this.displayCombat = isCombatActive;
      },
    );
    this.subscriptions.add(isCombatActiveSub);

    const playerCharactersSub =
      this.combatStateService.playerCharacters$.subscribe((entities) => {
        if (entities.length > 0) {
          this.playerCharacters = entities;
        }
      });
    this.subscriptions.add(playerCharactersSub);

    const enemyCharactersSub =
      this.combatStateService.enemyCharacters$.subscribe((entities) => {
        if (entities.length > 0) {
          this.enemyCharacters = entities;
        }
      });
    this.subscriptions.add(enemyCharactersSub);

    const nextCombatSub = this.combatStateService.nextCombat$.subscribe(
      (time) => {
        if (time == null) return;
        this.nextCombatIn = time;
        this.displayCombat = false;
      },
    );
    this.subscriptions.add(nextCombatSub);

    const combatResultSub = this.combatStateService.combatResult$.subscribe(
      (combatResult) => {
        if (combatResult == null) return;
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

    //Flavor text for when not in combat
    this.pickRandomFlavorText();

    // Optional: change it every few seconds
    setInterval(() => this.pickRandomFlavorText(), 5000);
  }

  ngOnDestroy() {
    this.subscriptions.unsubscribe();
  }

  initiateStoppingCombat() {
    this.isStoppingCombat = true;
    this.stopCombatButtonText = 'Stopping combat..';
    this.characterActionService.stopCharacterAction();
    // If we're seeing the "You're already in combat screen", and click to stop combat from there, it should call all the stop logic
    if (!this.displayCombat) this.stopCombat();
  }

  stopCombat() {
    this.subscriptions.unsubscribe();
    this.gameService.endCombat();
    this.characterActionService.clearCurrentAction();
  }

  combatEnded() {
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
      case EventType.Parry:
        this.handleBlockEvent(event);
        break;
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
      case EventType.RestoreMana:
        this.handleRestoreManaEvent(event);
        break;
      case EventType.RestoreBarrier:
        this.handleRestoreBarrierEvent(event);
        break;
      case EventType.Lifesteal:
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
      case EventType.StatusEffect:
        this.handleStatusEffectEvent(event);
        break;
      case EventType.StatusEffectExpired:
        const statusEffectExpired = true;
        this.handleStatusEffectEvent(event, statusEffectExpired);
        break;
      case EventType.Regeneration:
        this.handleRegeneration(event);
        break;
      // Add other event types as needed
      default:
        console.warn(`Unhandled event type: ${event.eventType}`);
    }
  }

  private handleAbilityUseEvent(event: CombatEvent) {
    this.updateCharacter(event.combatEntity);
  }

  private handleDamageEvent(event: CombatEvent): void {
    this.updateCharacter(event.combatEntity);
  }

  private handleHealEvent(event: CombatEvent): void {
    this.updateCharacter(event.combatEntity);
  }

  private handleRestoreManaEvent(event: CombatEvent): void {
    this.updateCharacter(event.combatEntity);
  }

  private handleRestoreBarrierEvent(event: CombatEvent): void {
    this.updateCharacter(event.combatEntity);
  }

  private handleMissEvent(event: CombatEvent): void {
    // Implement specific logic for handling block events
  }

  private handleBlockEvent(event: CombatEvent): void {
    // Implement specific logic for handling block events
  }

  private handleSummonEvent(event: CombatEvent): void {
    const summonedCharacter = event.combatEntity;
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
  }

  private handleBuffEvent(event: CombatEvent, buffExpired: boolean = false) {
    this.updateCharacter(event.combatEntity);
    // if (buffExpired) console.log(`Buff Expired: ${event.details}`);
    // else console.log(`Buff: ${event.details}`);
  }

  private handleDebuffEvent(event: CombatEvent, buffExpired: boolean = false) {
    this.updateCharacter(event.combatEntity);
    // if (buffExpired) console.log(`Buff Expired: ${event.details}`);
    // else console.log(`Buff: ${event.details}`);
  }

  handleStatusEffectEvent(
    event: CombatEvent,
    statusEffectExpired: boolean = false,
  ) {
    // if (buffExpired) console.log(`Buff Expired: ${event.details}`);
    // else console.log(`Buff: ${event.details}`);
  }

  handleRegeneration(event: CombatEvent) {
    this.updateCharacter(event.combatEntity);
  }

  private updateCharacter(combatEntity: SimpleCombatEntityDto) {
    const character = this.findCharacterById(combatEntity.id);
    if (!character) return;
    character.health = combatEntity.health;
    character.maxHealth = combatEntity.maxHealth;
    character.mana = combatEntity.mana;
    character.maxMana = combatEntity.maxMana;
    character.barrier = combatEntity.barrier;
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

  flavorMessages: string[] = [
    'Your blade clashes against steel!',
    'You dodge a heavy blow just in time!',
    'The battlefield roars around you!',
    'Your stance shifts as you parry another strike!',
    'A shadow lunges from the fog, and you counter!',
    'Sparks fly as weapons collide!',
    'You grit your teeth and press the attack!',
    'Blood spatters across your armor!',
    'You barely deflect a vicious strike!',
    'A roar erupts as your enemy charges!',
    'You lunge forward, blade aimed at their heart!',
    'Pain flares as you take a hit!',
    'Your opponent stumbles back from your blow!',
    'You circle, looking for an opening!',
    'Sweat drips into your eyes as the fight rages on!',
    'You unleash a flurry of rapid strikes!',
    'A powerful blow sends you skidding backward!',
    'Your shout echoes over the chaos of battle!',
    'You feel the rhythm of combat take hold!',
    'Your blade finds a gap in their defense!',
  ];

  flavorText: string = '';
  flavorTextVisible = false;

  pickRandomFlavorText(): void {
    this.flavorTextVisible = false;
    const newText =
      this.flavorMessages[
        Math.floor(Math.random() * this.flavorMessages.length)
      ];

    // avoid repeating the same text twice in a row
    if (newText === this.flavorText && this.flavorMessages.length > 1) {
      this.pickRandomFlavorText(); // try again
    } else {
      this.flavorText = newText;
    }
    setTimeout(() => {
      this.flavorTextVisible = true;
    });
  }
}
