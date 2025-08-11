import { Component, EventEmitter, Input, Output, signal } from '@angular/core';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';
import { NgFor, NgIf } from '@angular/common';

export type DungeonDifficulty = 'Normal' | 'Heroic' | 'Mythic';

export interface DungeonPreviewData {
  id: string;
  title: string;
  heroImage: string;
  lore: string;
  requiredLevel: number;
  dailyEntries?: number; // optional; show if present
  keyItem?: { icon: string; name: string; have: number; need: number };
  roomsRange?: [number, number]; // e.g., [5,8]
  estMinutes?: [number, number]; // e.g., [3,5]
  rewards: { icon: string; name: string }[];
  unlockedDifficulties: DungeonDifficulty[]; // which are available
}

@Component({
  selector: 'app-dungeon-preview',
  standalone: true,
  imports: [NgIf, NgFor, RegularButtonComponent],
  templateUrl: './dungeon-preview.component.html',
})
export class DungeonPreviewComponent {
  @Input({ required: true }) data!: DungeonPreviewData;
  @Output() backEvent = new EventEmitter<void>();

  difficulty = signal<DungeonDifficulty>('Normal');

  selectDifficulty(d: DungeonDifficulty) {
    if (this.data.unlockedDifficulties.includes(d)) this.difficulty.set(d);
  }

  back() {
    this.backEvent.emit();
  }
}
