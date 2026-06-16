import { Component, Input } from '@angular/core';
import { ColosseumMatchResult } from '../../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-record-of-battle',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DatePipe,
    CharacterTagComponent,
    NumberFormatPipe,
  ],
  templateUrl: './record-of-battle.component.html',
})
export class RecordOfBattleComponent {
  @Input() previousMatches: ColosseumMatchResult[] = [];
  readonly id;

  constructor(private state: CharacterStateService) {
    this.id = state.currentCharacterId();
  }

  get latestMatch(): ColosseumMatchResult | undefined {
    return this.previousMatches[0];
  }

  get ratingSwing(): number {
    return this.previousMatches.reduce((total, match) => {
      if (match.characterAId === this.id) {
        return (
          total +
          this.ratingDelta(
            match.characterARatingBefore,
            match.characterARatingAfter,
          )
        );
      }

      if (match.characterBId === this.id) {
        return (
          total +
          this.ratingDelta(
            match.characterBRatingBefore,
            match.characterBRatingAfter,
          )
        );
      }

      return total;
    }, 0);
  }

  ratingDelta(before: number, after: number): number {
    return after - before;
  }

  deltaClass(delta: number): string {
    if (delta > 0) return 'text-emerald-400';
    if (delta < 0) return 'text-rose-400';
    return 'text-zinc-200';
  }

  formatDelta(delta: number): string {
    return delta > 0 ? `+${delta}` : `${delta}`;
  }

  resultLabel(match: ColosseumMatchResult): string {
    if (!match.winnerId) return 'Draw';
    if (match.winnerId === this.id) return 'Victory';
    if (match.characterAId === this.id || match.characterBId === this.id) {
      return 'Defeat';
    }

    return 'Resolved';
  }

  resultClass(match: ColosseumMatchResult): string {
    const label = this.resultLabel(match);

    if (label === 'Victory') return 'border-emerald-500/60 text-emerald-300';
    if (label === 'Defeat') return 'border-rose-500/60 text-rose-300';
    if (label === 'Draw') return 'border-amber-500/60 text-amber-300';
    return 'border-light_gray/60 text-zinc-300';
  }
}
