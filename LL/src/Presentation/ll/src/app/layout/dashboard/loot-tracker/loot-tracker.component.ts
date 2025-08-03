import { NgFor, NgIf } from '@angular/common';
import { Component, signal } from '@angular/core';

export interface LootTrackerEntry {
  items: { name: string; amount: number }[];
}

@Component({
  selector: 'app-loot-tracker',
  standalone: true,
  imports: [NgIf, NgFor],
  templateUrl: './loot-tracker.component.html',
})
export class LootTrackerComponent {
  entries: LootTrackerEntry[] = [];
  expanded = signal(true);

  constructor() {
    // Example data
    this.entries = [
      {
        items: [
          { name: "Goblin's Essence", amount: 1 },
          { name: "Large Rat's Essence", amount: 1 },
        ],
      },
      {
        items: [{ name: 'Soulstones', amount: 2 }],
      },
    ];
  }

  toggle() {
    this.expanded.update((v) => !v);
  }
}
