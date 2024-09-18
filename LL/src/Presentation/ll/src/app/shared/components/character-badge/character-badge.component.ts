import { NgStyle } from '@angular/common';
import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-character-badge',
  standalone: true,
  imports: [NgStyle],
  templateUrl: './character-badge.component.html',
  styleUrl: './character-badge.component.css',
})
export class CharacterBadgeComponent {
  @Input() level: number = 1;
  @Input() currentExperience: number = 77;
  @Input() maxExperience: number = 1;

  calculateOffset(): string {
    const circumference = 2 * Math.PI * 15;
    const progress =
      (this.currentExperience / 100) /*<-- max experience */ * 100;
    const offset = circumference - (progress / 100) * circumference;
    return offset.toString();
  }
}
