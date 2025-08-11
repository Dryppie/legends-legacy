import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [RegularButtonComponent],
  templateUrl: './dungeon-card.component.html',
})
export class DungeonCardComponent {
  @Input() number!: number | string;
  @Input() title!: string;
  @Input() image!: string;
  @Input() requiredLabel = 'REQUIRED';
  @Input() requiredIcon = '/assets/icons/swords.svg';
  @Input() requiredValue!: string | number;

  @Input() height = 176;
  @Input() cornerSize = 32;

  @Output() enter = new EventEmitter<void>();

  onEnter(): void {
    this.enter.emit();
  }
}
