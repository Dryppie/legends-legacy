import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-character-badge',
  standalone: true,
  imports: [],
  templateUrl: './character-badge.component.html',
})
export class CharacterBadgeComponent {
  @Input() level?: number = 1;
  @Input() currentExperience?: number = 0;
  @Input() maxExperience?: number = 1;
  usableCircumference = 78.9;

  calculateOffset(): string {
    if (this.currentExperience == null || this.maxExperience == null) return '';
    const progress = this.currentExperience / this.maxExperience;

    // offset = (total dash) - (progress * total dash)
    const offset =
      this.usableCircumference - progress * this.usableCircumference;

    return offset.toString();
  }
}
