import {
  Component,
  effect,
  EventEmitter,
  Input,
  OnInit,
  Output,
} from '@angular/core';
import { CombatAvatarComponent } from './combat-avatar/combat-avatar.component';
import { CombatOverviewComponent } from './combat-overview/combat-overview.component';
import { CombatEvent, EventType } from '../../models/Dtos/combatEventDto';
import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  BattleOutcome,
  CombatResultDto,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../models/Dtos/combatResultDto';
import { Subscription } from 'rxjs';
import { CountdownComponent } from '../countdown/countdown.component';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CombatStateService } from '../../../core/state/combat-state/combat-state.service';
import { MiniButtonComponent } from '../custom-components/buttons/mini-button/mini-button.component';
import { CombatLogComponent } from './combat-log/combat-log.component';
import { BattleType } from '../../../core/state/combat-state/combatState';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { toSignal } from '@angular/core/rxjs-interop';
import { CombatEntityStatsComponent } from './combat-entity-stats/combat-entity-stats.component';
import { TourService } from '../../../core/services/client-side/tutorial-tour/tour.service';

@Component({
  selector: 'app-combat',
  standalone: true,
  imports: [
    CombatAvatarComponent,
    CombatOverviewComponent,
    NgClass,
    NgFor,
    NgIf,
    CountdownComponent,
    MiniButtonComponent,
    CombatLogComponent,
    CombatEntityStatsComponent,
  ],
  templateUrl: './combat.component.html',
})
export class CombatComponent implements OnInit {
  combatEvents: CombatEvent[] = [];
  entityStats: EntityStats[] = [];
  private lastEventsLength = 0;
  @Input() battleType: BattleType = BattleType.IdleCombat;
  @Output() skipBattle = new EventEmitter<void>();

  isStoppingCombat = false;
  nextCombatIn: Date | null = null;
  outcome: BattleOutcome | null = null;
  stopCombatButtonText: string = 'Stop Combat';

  playerCharacters: SimpleCombatEntityDto[] = [];
  enemyCharacters: SimpleCombatEntityDto[] = [];
  subscriptions: Subscription = new Subscription();
  readonly currentAction;
  // Only set to true if a combat result has been received, or if start combat has been
  displayCombat = false;
  isLoading = false;

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly gameService: GameService,
    public readonly combatStateService: CombatStateService,
    private readonly tour: TourService,
  ) {
    this.currentAction = this.characterActionService.currentAction;

    const isLoadingSig = this.characterActionService.loadingCombat;

    effect(() => {
      this.isLoading = isLoadingSig();
    });

    const isCombatActiveSig = toSignal(this.gameService.combatActive$, {
      initialValue: false,
    });

    effect(() => {
      this.displayCombat = isCombatActiveSig();
    });

    effect(() => {
      const players = this.combatStateService.getPlayerCharacters(
        this.battleType,
      )();
      if (players) this.playerCharacters = players;
    });

    effect(() => {
      const enemies = this.combatStateService.getEnemyCharacters(
        this.battleType,
      )();
      if (enemies) this.enemyCharacters = enemies;
    });

    effect(() => {
      const stats = this.combatStateService.getEntityStats(this.battleType)();
      if (stats) this.entityStats = stats;
    });

    /** Handle combat event stream */
    effect(() => {
      const allEvents = this.combatStateService.getCombatEvents(
        this.battleType,
      )();
      const previousLength = this.lastEventsLength;
      const newEvents = allEvents.slice(previousLength);
      this.lastEventsLength = allEvents.length;

      newEvents.forEach((event) => this.handleCombatEvent(event));
    });

    /** Handle next combat tick */
    effect(() => {
      const time = this.combatStateService.getNextCombat(this.battleType)();
      if (time) this.nextCombatIn = time;
      else this.nextCombatIn = this.currentAction()?.updatedAt ?? new Date();
    });

    /** Handle combat result */
    effect(() => {
      const result = this.combatStateService.getCombatResult(this.battleType)();
      if (result) {
        this.displayCombat = true;
        this.setupCombat();

        this.syncCharactersFromResult(result);
      }
    });

    /** Handle outcome to optionally trigger auto-exit */
    effect(() => {
      const outcome = this.combatStateService.getCombatOutcome(
        this.battleType,
      )();

      this.outcome = outcome;

      if (outcome && this.isStoppingCombat) {
        setTimeout(() => {
          this.stopCombat();
        }, 3000);
      }
    });
  }

  private syncCharactersFromResult(result: CombatResultDto) {
    // Update all player team members
    result.playerTeam.forEach((entity) => this.updateCharacter(entity));

    // Update all enemy team members
    result.enemyTeam.forEach((entity) => this.updateCharacter(entity));
  }

  ngOnInit(): void {
    // this.currentAction$ = this.characterActionService.currentAction$;

    // const isLoadingSub =
    //   this.characterActionService.loadingCombatAction$.subscribe(
    //     (isLoading) => {
    //       this.isLoading = isLoading;
    //     },
    //   );
    // this.subscriptions.add(isLoadingSub);

    this.playerCharacters = [
      {
        name: '',
        id: '',
        imagePath: '',
        health: 100,
        maxHealth: 100,
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
        barrier: 0,
      },
    ];

    this.pickRandomFlavorText();
    setInterval(() => this.pickRandomFlavorText(), 5000);
  }

  onStopOrSkip(): void {
    if (this.battleType === BattleType.IdleCombat) {
      // Original behaviour
      this.initiateStoppingCombat();
    } else if (this.battleType === BattleType.Colosseum) {
      // New behaviour
      this.skipCombat();
    }
  }

  skipCombat() {
    this.skipBattle.emit();
  }

  initiateStoppingCombat() {
    if (this.isStoppingCombat) return;
    this.isStoppingCombat = true;
    this.stopCombatButtonText = 'Stopping combat..';
    this.characterActionService.stopAction();
    // If we're seeing the "You're already in combat screen", and click to stop combat from there, it should call all the stop logic
    if (!this.displayCombat) this.stopCombat();
  }

  stopCombat() {
    this.subscriptions.unsubscribe();
    this.gameService.endCombat();
    this.characterActionService.clear();
  }

  setupCombat() {
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
      case EventType.HealthRegeneration:
        this.handleRegeneration(event);
        break;
      case EventType.Death:
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
  }

  private handleDebuffEvent(event: CombatEvent, buffExpired: boolean = false) {
    this.updateCharacter(event.combatEntity);
  }

  handleStatusEffectEvent(
    event: CombatEvent,
    statusEffectExpired: boolean = false,
  ) {}

  handleRegeneration(event: CombatEvent) {
    this.updateCharacter(event.combatEntity);
  }

  private updateCharacter(combatEntity: SimpleCombatEntityDto) {
    const character = this.findCharacterById(combatEntity.id);
    if (!character) return;
    character.health = combatEntity.health;
    character.maxHealth = combatEntity.maxHealth;
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

  battleTitle(): string {
    if (this.isLoading) return 'Loading Battle';

    if (this.battleType === BattleType.IdleCombat) {
      const areaName = this.currentAction()?.combatActionDetails?.area?.name;
      return areaName ? `Shenic - ${areaName}` : 'Idle Battle';
    }

    if (this.battleType === BattleType.Dungeon) return 'Dungeon Battle';
    if (this.battleType === BattleType.Colosseum) return 'Arena Battle';

    return 'Battle';
  }

  battleStatusLabel(): string {
    if (this.outcome) return this.outcome;
    if (this.isLoading) return 'Loading';
    return 'In Progress';
  }

  battleStatusClass(): Record<string, boolean> {
    return {
      'll-badge-success': this.outcome === BattleOutcome.Victory,
      'll-badge-danger': this.outcome === BattleOutcome.Defeat,
      'll-badge-accent':
        this.outcome === BattleOutcome.Draw ||
        (!this.outcome && !this.isLoading),
      'll-badge-muted': this.isLoading,
    };
  }

  teamSelectorClass(
    team: 'player' | 'enemy',
    index: number,
  ): Record<string, boolean> {
    const selected =
      team === 'player'
        ? this.selectedPlayerCharacterIndex === index
        : this.selectedEnemyCharacterIndex === index;

    return {
      'll-card-accent text-primary': selected && team === 'player',
      'll-card-danger text-danger': selected && team === 'enemy',
      'text-white': !selected,
    };
  }

  teamMemberLabel(entity: SimpleCombatEntityDto, index: number): string {
    return entity.name || `Unit ${index + 1}`;
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

  ngAfterViewInit(): void {
    this.waitForTourElements().then(() => {
      this.tour.start('combat');
    });
  }

  private async waitForTourElements(): Promise<void> {
    const maxAttempts = 20;
    const delay = 10; // ms

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
      const allExist = !!this.entityStats.length;
      if (allExist) return;
      await new Promise((resolve) => setTimeout(resolve, delay));
    }
  }
}
