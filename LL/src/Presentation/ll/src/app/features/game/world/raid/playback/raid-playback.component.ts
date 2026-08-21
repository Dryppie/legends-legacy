import { CommonModule } from '@angular/common';
import { Component, input, output } from '@angular/core';
import {
  RaidLane,
  RaidRun,
} from '../../../../../core/services/api/raid/raid.service';
import { RaidCombatFrame } from '../../../../../core/services/client-side/combat/raid-playback.service';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../../core/state/combat-state/combatState';
import { CombatComponent } from '../../../../../shared/components/combat/combat.component';

export interface RaidPreparationPlaybackView {
  lane: RaidLane;
  frame: RaidCombatFrame;
  progressPercent: number;
  elapsedSeconds: number;
  durationSeconds: number;
  friendlyAlive: number;
  friendlyTotal: number;
  hostileHealth: number;
  hostileMaxHealth: number;
  status: string;
  completed: boolean;
  isWaveTransitionHold: boolean;
}

@Component({
  selector: 'app-raid-playback',
  imports: [CommonModule, CombatComponent],
  templateUrl: './raid-playback.component.html',
  styleUrl: './raid-playback.component.scss',
})
export class RaidPlaybackComponent {
  readonly raid = input.required<RaidRun>();
  readonly playbackLane = input<RaidLane | null>(null);
  readonly showingAllPreparations = input(false);
  readonly preparationViews = input<readonly RaidPreparationPlaybackView[]>([]);
  readonly battleTitle = input.required<string>();
  readonly wingName = input.required<string>();
  readonly enemyName = input.required<string>();
  readonly preparationSummaryLocked = input(false);

  readonly closePlayback = output<void>();
  readonly focusLane = output<RaidLane>();
  readonly showAll = output<void>();
  readonly showOne = output<void>();

  readonly battleType = BattleType.Raid;

  constructor(readonly combatState: CombatStateService) {}

  raidDifficultyLabel(plusLevel: number): string {
    return plusLevel === 0 ? 'Regular' : `+${plusLevel}`;
  }

  raidEncounterName(lane: RaidLane | null): string {
    switch (lane) {
      case 'MainGuard':
        return 'Main Guard';
      case 'FinalAssault':
        return 'Final Assault';
      case 'Rearguard':
        return 'Rearguard';
      case 'Vanguard':
        return 'Vanguard';
      default:
        return 'Raid';
    }
  }

  preparationObjective(lane: RaidLane): string {
    switch (lane) {
      case 'Rearguard':
        return 'Defeat ten reinforcement waves';
      case 'Vanguard':
        return 'Break the raid guardian';
      case 'MainGuard':
        return 'Disrupt the boss projection';
      default:
        return 'Prepare for the Final Assault';
    }
  }

  preparationHostileName(view: RaidPreparationPlaybackView): string {
    if (view.lane === 'Rearguard' && view.frame.waveNumber !== null) {
      return `Reinforcements · Wave ${view.frame.waveNumber}`;
    }
    return (
      view.frame.hostile.map((entity) => entity.name).join(', ') || 'Objective'
    );
  }

  preparationEntityDamage(
    view: RaidPreparationPlaybackView,
    entityId: string,
  ): number {
    return (
      view.frame.entityStats.find((stats) => stats.entityId === entityId)
        ?.damageDone ?? 0
    );
  }

  healthPercent(health: number, maxHealth: number): number {
    return Math.max(0, Math.min(100, (health / Math.max(1, maxHealth)) * 100));
  }

  trackPreparationLane(
    _index: number,
    view: RaidPreparationPlaybackView,
  ): RaidLane {
    return view.lane;
  }

  trackCombatEntity(_index: number, entity: { id: string }): string {
    return entity.id;
  }
}
