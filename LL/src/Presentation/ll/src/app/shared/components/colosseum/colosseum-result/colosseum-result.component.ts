import { NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-colosseum-result',
  standalone: true,
  imports: [NgClass, RegularButtonComponent],
  templateUrl: './colosseum-result.component.html',
})
export class ColosseumResultComponent {
  @Input() outcome: 'Victory' | 'Defeat' | 'Draw' | null = null;
  @Output() closed = new EventEmitter<void>();

  get title(): string {
    switch (this.outcome) {
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

  get colorClasses(): string {
    switch (this.outcome) {
      case 'Victory':
        return 'll-badge-success';
      case 'Defeat':
        return 'll-badge-danger';
      case 'Draw':
        return 'll-badge-warning';
      default:
        return '';
    }
  }
}
