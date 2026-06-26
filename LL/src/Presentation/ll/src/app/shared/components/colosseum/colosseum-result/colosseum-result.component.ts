import { NgClass, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { StartArenaBattleResponse } from '../../../models/Dtos/colosseum/startArenaBattleResponse';
import { NumberFormatPipe } from '../../../pipes/number-format/number-format.pipe';

@Component({
  selector: 'app-colosseum-result',
  standalone: true,
  imports: [NgClass, NgIf, RegularButtonComponent, NumberFormatPipe],
  templateUrl: './colosseum-result.component.html',
})
export class ColosseumResultComponent {
  @Input() result: StartArenaBattleResponse | null = null;
  @Output() closed = new EventEmitter<void>();

  get title(): string {
    switch (this.result?.outcome.result) {
      case 'Victory':
        return 'Victory!';
      case 'Defeat':
        return 'Defeated...';
      case 'Draw':
        return 'Draw';
      default:
        return '';
    }
  }

  get cardClass(): string {
    switch (this.result?.outcome.result) {
      case 'Victory':
        return 'll-card-success';
      case 'Defeat':
        return 'll-card-danger';
      case 'Draw':
        return 'll-card-warning';
      default:
        return '';
    }
  }

  get titleClass(): string {
    switch (this.result?.outcome.result) {
      case 'Victory':
        return 'll-text-success';
      case 'Defeat':
        return 'll-text-danger';
      case 'Draw':
        return 'll-text-warning';
      default:
        return 'text-primary';
    }
  }

  formatDelta(delta: number): string {
    return delta > 0 ? `+${delta}` : `${delta}`;
  }

  get deltaClass(): string {
    const delta = this.result?.attackerRating.delta ?? 0;
    if (delta > 0) return 'll-text-success';
    if (delta < 0) return 'll-text-danger';
    return 'll-text-muted';
  }
}
