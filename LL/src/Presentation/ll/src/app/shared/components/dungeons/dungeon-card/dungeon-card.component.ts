import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import {
  DungeonPreviewComponent,
  DungeonPreviewData,
} from '../dungeon-preview/dungeon-preview.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-dungeon-card',
  standalone: true,
  imports: [NgIf, DungeonPreviewComponent, RegularButtonComponent],
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

  // In a real app, fetch this by id. Hard-coded example for Goblin Mines:
  previewData: DungeonPreviewData = {
    id: 'goblin-mines',
    title: 'Goblin Mines',
    heroImage: 'entities/optimized/hobgoblin.webp',
    lore: 'The goblins have mined deep into cursed stone, guarding ancient relics.',
    requiredLevel: 5,
    dailyEntries: 1,
    keyItem: {
      icon: '/assets/icons/key-goblin.svg',
      name: 'Goblin Key',
      have: 0,
      need: 1,
    },
    roomsRange: [5, 8],
    estMinutes: [3, 5],
    unlockedDifficulties: ['Normal'], // only Normal unlocked in this example
    rewards: [
      { icon: '/assets/icons/sword.svg', name: 'Goblin Cleaver' },
      { icon: '/assets/icons/gem-green.svg', name: 'Viridian Shard' },
      { icon: '/assets/icons/scroll.svg', name: 'Rune Scroll' },
      { icon: '/assets/icons/essence-purple.svg', name: 'Goblin Essence' },
    ],
  };

  showPreview = signal(false);

  openPreview() {
    this.showPreview.set(true);
  }
  closePreview() {
    this.showPreview.set(false);
  }
}
