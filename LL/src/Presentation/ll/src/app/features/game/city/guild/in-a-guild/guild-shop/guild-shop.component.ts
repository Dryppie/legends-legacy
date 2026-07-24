import { Component } from '@angular/core';
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
    templateUrl: './guild-shop.component.html'
})
export class GuildShopComponent {
  readonly shop;
  readonly loading;
  readonly stockTypes: GuildShopStockType[] = ['Common', 'Weekly', 'Prestige'];

  constructor(private readonly state: GuildStateService) {
    this.shop = this.state.shop;
    this.loading = this.state.loading;
  }

  purchase(item: GuildShopItem): void {
    if (!item.canPurchase) return;
    this.state.purchaseShopItem(item.key);
  }

  stockClass(stockType: string): string {
    switch (stockType) {
      case 'Prestige':
        return 'll-badge-accent';
      case 'Weekly':
        return 'll-badge-success';
      default:
        return 'll-badge-muted';
    }
  }

  itemsForStock(stockType: GuildShopStockType): GuildShopItem[] {
    return this.shop()?.items.filter((item) => item.stockType === stockType) ?? [];
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
      requirements.push(`${item.requiredWeeklyContribution.toLocaleString()} weekly contribution`);
    }
    return requirements.length > 0 ? requirements.join(' / ') : 'No requirement';
  }

  rewardLabel(reward: GuildShopReward): string {
    return reward.name?.trim() || reward.key?.trim() || reward.type;
  }

  rewardAmountLabel(reward: GuildShopReward): string {
    if (reward.type === 'Item' || reward.type === 'Title') {
      return reward.amount > 1 ? `${reward.amount.toLocaleString()} copies` : 'Unlock';
    }

    return reward.amount.toLocaleString();
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
