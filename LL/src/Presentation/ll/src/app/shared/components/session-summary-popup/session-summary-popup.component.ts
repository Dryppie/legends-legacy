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

interface RewardMetric {
  label: string;
  value: number;
}

interface RewardSection {
  key: string;
  title: string;
  description: string;
  metrics: RewardMetric[];
  items: LootSummaryItem[];
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
    const toDate = new Date(to);
    const diffMs = Math.max(0, toDate.getTime() - fromDate.getTime());

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

  rewardSections(combatSession: CombatSessionDto): RewardSection[] {
    const summary = combatSession.combatSummary;
    const rewards = summary.rewardBreakdown;

    const sections: RewardSection[] = [
      {
        key: 'power',
        title: 'Power',
        description: 'Character growth and direct upgrades',
        metrics: this.positiveMetrics([
          { label: 'Character XP', value: summary.totalExperience },
        ]),
        items: this.compactLoot(rewards?.powerItems ?? []),
      },
      {
        key: 'crafting',
        title: 'Crafting',
        description: 'Materials for forging and professions',
        metrics: [],
        items: this.compactLoot(rewards?.craftingItems ?? []),
      },
      {
        key: 'essence',
        title: 'Essence',
        description: 'Essence drops and progression materials',
        metrics: [],
        items: this.compactLoot(rewards?.essenceItems ?? []),
      },
      {
        key: 'dungeon-access',
        title: 'Dungeon Access',
        description: 'Sigils used to enter Dungeons',
        metrics: [],
        items: this.compactLoot(rewards?.dungeonAccessItems ?? []),
      },
      {
        key: 'currencies',
        title: 'Currencies',
        description: 'Spendable progression currencies',
        metrics: this.positiveMetrics([
          { label: 'Cinders', value: summary.totalCinders },
          { label: 'Soulstones', value: summary.totalSoulstones },
        ]),
        items: [],
      },
    ];

    return sections.filter(
      (section) => section.metrics.length > 0 || section.items.length > 0,
    );
  }

  hasRewards(combatSession: CombatSessionDto): boolean {
    return this.rewardSections(combatSession).length > 0;
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

  private positiveMetrics(metrics: RewardMetric[]): RewardMetric[] {
    return metrics.filter((metric) => metric.value > 0);
  }
}
