import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TowerHallOfFameEntry,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';
import { CombatService } from '../../../../../core/services/client-side/combat/combat.service';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { BattleType } from '../../../../../core/state/combat-state/combatState';
import { CombatComponent } from '../../../../../shared/components/combat/combat.component';

@Component({
  selector: 'app-tower-hall-of-fame',
  imports: [CommonModule, RouterLink, CombatComponent],
  templateUrl: './tower-hall-of-fame.component.html',
  styleUrl: '../tower-page.scss',
})
export class TowerHallOfFameComponent implements OnInit {
  private readonly tower = inject(WorldTowerService);
  private readonly combat = inject(CombatService);
  readonly combatState = inject(CombatStateService);
  readonly battleType = BattleType.Tower;
  readonly records = signal<TowerHallOfFameEntry[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly replayingAttemptId = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.tower
      .getHallOfFame()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (records) => this.records.set(records),
        error: (error) =>
          this.error.set(
            (error as { errorMessage?: string })?.errorMessage ??
              'Server history could not be read.',
          ),
      });
  }

  duration(seconds: number): string {
    const total = Math.max(0, seconds);
    return `${Math.floor(total / 60)}:${(total % 60).toString().padStart(2, '0')}`;
  }

  rosterSummary(record: TowerHallOfFameEntry): string {
    return record.participants.map((entry) => entry.characterName).join(', ');
  }

  floorLabel(floorNumber: number): string {
    return floorNumber.toString().padStart(2, '0');
  }

  replay(record: TowerHallOfFameEntry): void {
    if (this.replayingAttemptId()) return;

    this.replayingAttemptId.set(record.attemptId);
    this.error.set(null);
    this.tower
      .getAttemptCombatResult(record.attemptId)
      .pipe(finalize(() => this.replayingAttemptId.set(null)))
      .subscribe({
        next: (combatResult) =>
          this.combat.startTowerBattleSummary({ ...combatResult }),
        error: (error) =>
          this.error.set(
            (error as { errorMessage?: string })?.errorMessage ??
              'This first-clear replay could not be loaded.',
          ),
      });
  }

  closeReplay(): void {
    this.combat.closeCurrentTowerBattle();
  }
}
