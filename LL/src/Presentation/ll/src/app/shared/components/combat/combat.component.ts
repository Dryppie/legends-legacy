import {
  Component,
  computed,
  effect,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { CombatEvent } from '../../models/Dtos/combatEventDto';
import { NgClass, NgIf } from '@angular/common';
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
import { CombatEntityStatsComponent } from './combat-entity-stats/combat-entity-stats.component';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import { Router } from '@angular/router';
import { HelpLauncherComponent } from '../../help/help-launcher.component';
import { GUIDE_PAGE_IDS } from '../../help/guide-catalog';
import { CharacterActionType } from '../../models/enums/characterActionType';

@Component({
  selector: 'app-combat',
  host: { class: 'flex h-full min-h-0 w-full' },
  imports: [
    NgClass,
    NgIf,
    CountdownComponent,
    MiniButtonComponent,
    CombatLogComponent,
    CombatEntityStatsComponent,
    HelpLauncherComponent,
  ],
  templateUrl: './combat.component.html',
})
export class CombatComponent implements OnInit, OnDestroy {
  readonly combatGuidePageId = GUIDE_PAGE_IDS.combat;
  combatEvents: CombatEvent[] = [];
  entityStats: EntityStats[] = [];
  combatDurationTicks = 0;
  private readonly lastHandledCombatEvent = new Map<BattleType, CombatEvent>();
  private flavorIntervalId: ReturnType<typeof setInterval> | null = null;
  private flavorVisibilityTimeoutId: ReturnType<typeof setTimeout> | null =
    null;
  private readonly battleTypeSignal = signal<BattleType>(BattleType.IdleCombat);

  @Input()
  set battleType(value: BattleType) {
    this.battleTypeSignal.set(value ?? BattleType.IdleCombat);
  }

  get battleType(): BattleType {
    return this.battleTypeSignal();
  }

  @Output() skipBattle = new EventEmitter<void>();

  isStoppingCombat = false;
  nextCombatIn: Date | null = null;
  outcome: BattleOutcome | null = null;
  stopCombatButtonText: string = 'Stop Combat';

  playerCharacters: SimpleCombatEntityDto[] = [];
  enemyCharacters: SimpleCombatEntityDto[] = [];
  subscriptions: Subscription = new Subscription();
  readonly currentAction;
  readonly hasActiveIdleCombat;
  readonly idleCombatError;
  // Only set to true if a combat result has been received, or if start combat has been
  displayCombat = false;
  isLoading = false;

  combatDurationLabel(): string {
    const totalSeconds = Math.max(0, Math.round(this.combatDurationTicks / 10));
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    return minutes > 0 ? `${minutes}m ${seconds}s` : `${seconds}s`;
  }

  constructor(
    private readonly characterActionService: CharacterActionsStateService,
    private readonly gameService: GameService,
    public readonly combatStateService: CombatStateService,
    private readonly tour: FirstPartyTourService,
    private readonly router: Router,
  ) {
    this.currentAction = this.characterActionService.currentAction;
    this.hasActiveIdleCombat = computed(() => {
      const action = this.currentAction();
      return (
        action?.characterActionType === CharacterActionType.Combat &&
        !action.isDeleted
      );
    });
    this.idleCombatError = this.characterActionService.idleCombatError;

    const isStartingCombatSig = this.characterActionService.loadingCombat;
    const isRefreshingActionSig =
      this.characterActionService.loadingActionRefresh;

    effect(() => {
      this.isLoading = isStartingCombatSig() || isRefreshingActionSig();
    });

    effect(() => {
      const shouldLeaveCombatRoute =
        this.battleTypeSignal() === BattleType.IdleCombat &&
        !this.hasActiveIdleCombat() &&
        !isStartingCombatSig() &&
        !isRefreshingActionSig();

      if (!shouldLeaveCombatRoute) return;

      queueMicrotask(() => {
        if (this.router.url.startsWith('/game/combat')) {
          void this.router.navigate(['/game/world']);
        }
      });
    });

    effect(() => {
      const type = this.battleTypeSignal();
      this.displayCombat =
        type === BattleType.IdleCombat
          ? !!this.combatStateService.getCombatResult(type)()?.playerTeam.length
          : this.combatStateService.getIsCombatActive(type)();
    });

    effect(() => {
      const type = this.battleTypeSignal();
      const players = this.combatStateService.getPlayerCharacters(type)();
      if (players) this.playerCharacters = players;
    });

    effect(() => {
      const type = this.battleTypeSignal();
      const enemies = this.combatStateService.getEnemyCharacters(type)();
      if (enemies) this.enemyCharacters = enemies;
    });

    effect(() => {
      const type = this.battleTypeSignal();
      const stats = this.combatStateService.getEntityStats(type)();
      if (stats) this.entityStats = stats;
    });
    /** Handle next combat tick */
    effect(() => {
      const type = this.battleTypeSignal();
      const time = this.combatStateService.getNextCombat(type)();
      if (time) this.nextCombatIn = time;
      else this.nextCombatIn = this.currentAction()?.updatedAt ?? new Date();
    });

    /** Handle combat result */
    effect(() => {
      const type = this.battleTypeSignal();
      const result = this.combatStateService.getCombatResult(type)();
      this.combatDurationTicks = result?.duration ?? 0;
      if (result?.playerTeam.length) {
        this.syncCharactersFromResult(result);
      }
    });

    /** Handle outcome to optionally trigger auto-exit */
    effect(() => {
      const type = this.battleTypeSignal();
      const outcome = this.combatStateService.getCombatOutcome(type)();

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

    this.pickRandomFlavorText();
    this.flavorIntervalId = setInterval(
      () => this.pickRandomFlavorText(),
      5000,
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
    if (this.flavorIntervalId) clearInterval(this.flavorIntervalId);
    if (this.flavorVisibilityTimeoutId) {
      clearTimeout(this.flavorVisibilityTimeoutId);
    }
  }

  onStopOrSkip(): void {
    if (this.battleType === BattleType.IdleCombat) {
      this.initiateStoppingCombat();
    } else if (
      this.battleType === BattleType.Colosseum ||
      this.battleType === BattleType.Dungeon ||
      this.battleType === BattleType.Training
    ) {
      this.skipCombat();
    }
  }

  combatActionButtonText(): string {
    if (this.battleType === BattleType.IdleCombat) {
      return this.isStoppingCombat ? 'Quitting...' : 'Quit';
    }

    if (this.isEscapeDismissibleBattleType()) {
      return 'Close Summary (Esc)';
    }

    return 'Close Summary';
  }

  combatActionButtonMobileText(): string {
    return this.isEscapeDismissibleBattleType() ? 'Close Summary' : '';
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (!this.displayCombat || !this.outcome) return;
    if (!this.isEscapeDismissibleBattleType()) return;

    this.skipCombat();
  }

  outcomeBadgeClass(): string {
    switch (this.outcome) {
      case BattleOutcome.Victory:
        return 'll-badge-success';
      case BattleOutcome.Defeat:
        return 'll-badge-danger';
      case BattleOutcome.Draw:
        return 'll-badge-warning';
      default:
        return 'll-badge-muted';
    }
  }

  skipCombat() {
    this.skipBattle.emit();
  }

  private isEscapeDismissibleBattleType(): boolean {
    return (
      this.battleType === BattleType.Colosseum ||
      this.battleType === BattleType.Dungeon
    );
  }

  initiateStoppingCombat() {
    if (this.isStoppingCombat) return;
    this.isStoppingCombat = true;
    this.stopCombatButtonText = 'Stopping combat..';
    this.characterActionService.stopAction();
    this.gameService.endCombat();
  }

  stopCombat() {
    this.subscriptions.unsubscribe();
    this.gameService.endCombat();
    this.characterActionService.clear();
    if (this.battleType === BattleType.IdleCombat) {
      this.router.navigate(['/game/world']);
    }
  }

  retryOfflineProgress(): void {
    this.characterActionService.retryIdleCombatResolution();
  }

  private updateCharacter(
    combatEntity: SimpleCombatEntityDto | null | undefined,
  ) {
    if (!combatEntity?.id) return;

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

  battleTitle(): string {
    if (this.isLoading) return 'Resolving Combat';

    if (this.battleType === BattleType.IdleCombat) {
      const areaName = this.currentAction()?.combatActionDetails?.area?.name;
      return areaName ? `Shenic — ${areaName}` : 'Idle Battle';
    }

    if (this.battleType === BattleType.Dungeon) return 'Dungeon Battle';
    if (this.battleType === BattleType.Colosseum) return 'Arena Battle';
    if (this.battleType === BattleType.Training) return 'Training Battle';

    return 'Battle';
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
    if (this.flavorVisibilityTimeoutId) {
      clearTimeout(this.flavorVisibilityTimeoutId);
    }

    this.flavorVisibilityTimeoutId = setTimeout(() => {
      this.flavorTextVisible = true;
    });
  }

  ngAfterViewInit(): void {
    this.waitForTourElements().then(() => {
      if (this.battleType === BattleType.Training) {
        this.tour.start('tutorial-combat');
      }
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
