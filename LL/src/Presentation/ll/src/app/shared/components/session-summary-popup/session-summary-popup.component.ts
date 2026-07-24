import { NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';
import { CombatSessionDto } from '../../models/Dtos/combatResultDto';
import { InventoryItem } from '../../models/inventoryItem';

interface LootSummaryItem {
  key: string;
  name: string;
  quantity: number;
}

@Component({
    selector: 'app-session-summary-popup',
    imports: [NgIf, NgFor],
    templateUrl: './session-summary-popup.component.html'
})
export class SessionSummaryPopupComponent {
  constructor(public svc: SessionSummaryService) {}

  getDuration(from: string | Date, to: string | Date): string {
    const fromDate = new Date(from);
    const toDate = new Date();
    const diffMs = toDate.getTime() - fromDate.getTime();

    const totalMinutes = Math.floor(diffMs / (1000 * 60));
    const days = Math.floor(totalMinutes / (60 * 24));
    const hours = Math.floor((totalMinutes % (60 * 24)) / 60);
    const minutes = totalMinutes % 60;

    const parts: string[] = [];
    if (days) parts.push(`${days} day${days !== 1 ? 's' : ''}`);
    if (hours) parts.push(`${hours} hour${hours !== 1 ? 's' : ''}`);
    if (minutes || parts.length === 0)
      parts.push(`${minutes} minute${minutes !== 1 ? 's' : ''}`);
    let joinedDuration = parts.join(', ');
    if (days || hours >= 16) joinedDuration += ' (Rewards stop after 24 hours)';
    return joinedDuration;
  }

  gatheredLoot(combatSession: CombatSessionDto): LootSummaryItem[] {
    return this.compactLoot(
      combatSession.combatResult.gatheringRewards.flatMap(
        (gathering) => gathering.itemsGained,
      ),
    );
  }

  hasGatheredLoot(combatSession: CombatSessionDto): boolean {
    return combatSession.combatResult.gatheringRewards.some(
      (gathering) => gathering.itemsGained.length > 0,
    );
  }

  trackLoot(_index: number, loot: LootSummaryItem): string {
    return loot.key;
  }

  private compactLoot(items: InventoryItem[]): LootSummaryItem[] {
    const compacted = new Map<string, LootSummaryItem>();

    for (const item of items) {
      const itemBase = item.itemInstance.itemBase;
      const name = item.itemInstance.displayName || itemBase.name;
      const key = itemBase.id || name;
      const existing = compacted.get(key);

      if (existing) {
        existing.quantity += item.quantity;
        continue;
      }

      compacted.set(key, {
        key,
        name,
        quantity: item.quantity,
      });
    }

    return Array.from(compacted.values()).sort((a, b) =>
      a.name.localeCompare(b.name),
    );
  }
}
