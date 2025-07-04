import { NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RegularButtonComponent } from '../../buttons/regular-button/regular-button.component';

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
        return 'text-primary border-primary/30';
      case 'Defeat':
        return 'text-rose-500 border-rose-500/30';
      case 'Draw':
        return 'text-light_gray border-light_gray';
      default:
        return '';
    }
  }
}
