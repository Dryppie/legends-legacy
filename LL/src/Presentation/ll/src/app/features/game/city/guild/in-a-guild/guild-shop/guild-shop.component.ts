import { Component, computed, effect, signal } from '@angular/core';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import {
  GuildShopItem,
  GuildShopReward,
  GuildShopStockType,
} from '../../../../../../shared/models/Dtos/guild/guildShop';
import { NumberFormatPipe } from '../../../../../../shared/pipes/number-format/number-format.pipe';
import { HumanizeEnumPipe } from '../../../../../../shared/pipes/enums/humanize-enum.pipe';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-guild-shop',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DatePipe,
    NumberFormatPipe,
    HumanizeEnumPipe,
    RegularButtonComponent,
  ],
  templateUrl: './guild-shop.component.html',
  styleUrl: './guild-shop.component.scss',
})
export class GuildShopComponent {
  readonly shop;
  readonly loading;
  readonly stockTypes: GuildShopStockType[] = ['Common', 'Rare'];
  readonly selected = signal<GuildShopItem | null>(null);

  readonly selectedCost = computed(() => {
    const item = this.selected();
    return item?.guildFavorCost ?? 0;
  });

  readonly selectedBalance = computed(() => this.shop()?.guildFavor ?? 0);

  readonly selectedBalanceAfterPurchase = computed(() =>
    Math.max(0, this.selectedBalance() - this.selectedCost()),
  );

  readonly purchaseButtonText = computed(() => {
    if (!this.selected()) return 'Purchase';
    return `Purchase for ${this.selectedCost().toLocaleString()} Favor`;
  });

  constructor(private readonly state: GuildStateService) {
    this.shop = this.state.shop;
    this.loading = this.state.loading;

    effect(() => {
      const items = this.shop()?.items ?? [];
      const current = this.selected();
      if (items.length === 0) {
        this.selected.set(null);
        return;
      }

      const refreshed = current
        ? items.find((item) => item.key === current.key)
        : items[0];
      this.selected.set(refreshed ?? items[0]);
    });
  }

  select(item: GuildShopItem): void {
    this.selected.set(item);
  }

  isSelected(item: GuildShopItem): boolean {
    return this.selected()?.key === item.key;
  }

  purchase(item: GuildShopItem): void {
    if (!item.canPurchase) return;
    this.state.purchaseShopItem(item.key);
  }

  stockClass(stockType: string): string {
    switch (stockType) {
      case 'Rare':
        return 'll-badge-accent';
      default:
        return 'll-badge-muted';
    }
  }

  itemsForStock(stockType: GuildShopStockType): GuildShopItem[] {
    return (
      this.shop()?.items.filter((item) => item.stockType === stockType) ?? []
    );
  }

  hasStock(stockType: GuildShopStockType): boolean {
    return this.itemsForStock(stockType).length > 0;
  }

  requirementText(item: GuildShopItem): string {
    const requirements: string[] = [];
    if (item.requiredMarketOfficeLevel > 0) {
      requirements.push(`Market Office ${item.requiredMarketOfficeLevel}`);
    }
    if (item.requiredWeeklyContribution > 0) {
      requirements.push(
        `${item.requiredWeeklyContribution.toLocaleString()} weekly contribution`,
      );
    }
    return requirements.length > 0
      ? requirements.join(' / ')
      : 'No requirement';
  }

  rewardLabel(reward: GuildShopReward): string {
    return reward.name?.trim() || reward.key?.trim() || reward.type;
  }

  rewardAmountLabel(reward: GuildShopReward): string {
    if (
      reward.type === 'Title' ||
      (reward.type === 'Item' && reward.key?.startsWith('blueprint_'))
    ) {
      return 'Unlock';
    }

    return reward.amount.toLocaleString();
  }

  remainingThisWeek(item: GuildShopItem): number {
    return Math.max(0, item.weeklyLimit - item.purchasedThisPeriod);
  }

  resetLabel(resetAt: string): string {
    const reset = new Date(resetAt).getTime();
    const diff = reset - Date.now();
    if (Number.isNaN(reset) || diff <= 0) return 'soon';

    const hours = Math.ceil(diff / 3_600_000);
    const days = Math.floor(hours / 24);
    const remainingHours = hours % 24;
    if (days <= 0) return `${hours}h`;
    if (remainingHours === 0) return `${days}d`;
    return `${days}d ${remainingHours}h`;
  }
}
