import { NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { SessionSummaryService } from '../../../core/services/client-side/session-summary/session-summary.service';
import { CombatSessionDto } from '../../models/Dtos/combatResultDto';
import { InventoryItem } from '../../models/inventoryItem';
import { ItemInstance } from '../../models/item';
import { Rarity } from '../../models/enums/rarity';
import { ItemComponent } from '../item/item.component';

interface LootSummaryItem {
  key: string;
  itemInstance: ItemInstance;
  quantity: number;
  isRare: boolean;
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
  imports: [NgIf, NgFor, ItemComponent],
  templateUrl: './session-summary-popup.component.html',
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
    const gatheringRewards = combatSession.combatResult.gatheringRewards ?? [];
    const gatheringItems = gatheringRewards.flatMap(
      (reward) => reward.itemsGained ?? [],
    );

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
        key: 'gathering',
        title: 'Gathering',
        description:
          'Profession progress, gathered materials, and rare Catalysts',
        metrics: this.gatheringMetrics(gatheringRewards),
        items: this.compactLoot(gatheringItems),
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

  private gatheringMetrics(
    rewards: CombatSessionDto['combatResult']['gatheringRewards'],
  ): RewardMetric[] {
    const experienceByProfession = new Map<string, number>();

    for (const reward of rewards ?? []) {
      experienceByProfession.set(
        reward.toolType,
        (experienceByProfession.get(reward.toolType) ?? 0) +
          (reward.experienceGained ?? 0),
      );
    }

    return Array.from(experienceByProfession, ([profession, value]) => ({
      label: `${profession} XP`,
      value,
    })).filter((metric) => metric.value > 0);
  }

  hasRewards(combatSession: CombatSessionDto): boolean {
    return this.rewardSections(combatSession).length > 0;
  }

  trackLoot(_index: number, loot: LootSummaryItem): string {
    return loot.key;
  }

  trackSection(_index: number, section: RewardSection): string {
    return section.key;
  }

  private compactLoot(items: InventoryItem[]): LootSummaryItem[] {
    const compacted = new Map<string, LootSummaryItem>();

    for (const item of items) {
      const itemBase = item.itemInstance.itemBase;
      const key = itemBase.id || item.itemInstance.displayName || itemBase.name;
      const existing = compacted.get(key);

      if (existing) {
        existing.quantity += item.quantity;
        continue;
      }

      compacted.set(key, {
        key,
        itemInstance: item.itemInstance,
        quantity: item.quantity,
        isRare: ![Rarity.Common, Rarity.Uncommon].includes(
          item.itemInstance.itemBase.rarity,
        ),
      });
    }

    return Array.from(compacted.values()).sort((a, b) =>
      this.itemName(a.itemInstance).localeCompare(
        this.itemName(b.itemInstance),
      ),
    );
  }

  private itemName(item: ItemInstance): string {
    return item.displayName || item.itemBase.name;
  }

  private positiveMetrics(metrics: RewardMetric[]): RewardMetric[] {
    return metrics.filter((metric) => metric.value > 0);
  }
}
