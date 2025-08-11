import { Component, signal } from '@angular/core';
import { DungeonCardComponent } from '../../../../../shared/components/dungeons/dungeon-card/dungeon-card.component';
import {
  DungeonPreviewComponent,
  DungeonPreviewData,
} from '../../../../../shared/components/dungeons/dungeon-preview/dungeon-preview.component';
import { NgIf } from '@angular/common';

@Component({
  selector: 'app-dungeons',
  standalone: true,
  imports: [NgIf, DungeonCardComponent, DungeonPreviewComponent],
  templateUrl: './dungeons.component.html',
})
export class DungeonsComponent {
  showPreview = signal(false);

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

  openPreview() {
    this.showPreview.set(true);
  }
  closePreview() {
    this.showPreview.set(false);
  }
}
