import { Component, Input } from '@angular/core';
import { ColosseumMatchResult } from '../../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';
import { FilterTabsComponent } from '../../../../../shared/components/custom-components/tabs/filter-tabs/filter-tabs.component';

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
    FilterTabsComponent,
  ],
  templateUrl: './record-of-battle.component.html',
})
export class RecordOfBattleComponent {
  @Input() previousMatches: ColosseumMatchResult[] = [];
  readonly id;
  filter: 'all' | 'attacks' | 'defenses' = 'all';
  readonly filters = ['All', 'Attacks', 'Defenses'];

  constructor(private state: CharacterStateService) {
    this.id = state.currentCharacterId();
  }

  get latestMatch(): ColosseumMatchResult | undefined {
    return this.filteredMatches[0];
  }

  get filteredMatches(): ColosseumMatchResult[] {
    if (this.filter === 'attacks') {
      return this.previousMatches.filter((match) => match.characterAId === this.id);
    }

    if (this.filter === 'defenses') {
      return this.previousMatches.filter((match) => match.characterBId === this.id);
    }

    return this.previousMatches;
  }

  get ratingSwing(): number {
    return this.filteredMatches.reduce((total, match) => {
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
    if (delta > 0) return 'll-text-success';
    if (delta < 0) return 'll-text-danger';
    return 'll-text-muted';
  }

  formatDelta(delta: number): string {
    return delta > 0 ? `+${delta}` : `${delta}`;
  }

  setFilter(filter: 'all' | 'attacks' | 'defenses'): void {
    this.filter = filter;
  }

  setFilterTab(tab: string): void {
    this.setFilter(tab.toLowerCase() as 'all' | 'attacks' | 'defenses');
  }

  get activeFilterTab(): string {
    return this.filter[0].toUpperCase() + this.filter.slice(1);
  }

  isAttack(match: ColosseumMatchResult): boolean {
    return match.characterAId === this.id;
  }

  gloryEarned(match: ColosseumMatchResult): number {
    return this.isAttack(match)
      ? match.characterAGloryEarned
      : match.characterBGloryEarned;
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

    if (label === 'Victory') return 'll-badge-success';
    if (label === 'Defeat') return 'll-badge-danger';
    if (label === 'Draw') return 'll-badge-warning';
    return 'll-badge-muted';
  }
}
