import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
import { Component, computed, effect, signal } from '@angular/core';
import { ColosseumStateService } from '../../../../../core/services/api/colosseum/colosseum-state.service';
import { ChampionMarketItem } from '../../../../../shared/models/Dtos/colosseum/championMarket';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

interface ChampionMarketReward {
  amount: string;
  label: string;
}

@Component({
  selector: 'app-champions-market',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    DatePipe,
    NumberFormatPipe,
    RegularButtonComponent,
  ],
  templateUrl: './champions-market.component.html',
  styleUrl: './champions-market.component.scss',
})
export class ChampionsMarketComponent {
  readonly selected = signal<ChampionMarketItem | null>(null);

  readonly categories = computed(() => [
    ...new Set(
      (this.state.championMarket()?.items ?? []).map((item) => item.category),
    ),
  ]);

  readonly selectedCost = computed(() => this.selected()?.gloryCost ?? 0);

  readonly selectedBalance = computed(
    () => this.state.championMarket()?.glory ?? this.state.status()?.glory ?? 0,
  );

  readonly selectedBalanceAfterPurchase = computed(() =>
    Math.max(0, this.selectedBalance() - this.selectedCost()),
  );

  readonly purchaseButtonText = computed(() => {
    if (!this.selected()) return 'Purchase';
    return `Purchase for ${this.selectedCost().toLocaleString()} Glory`;
  });

  constructor(public readonly state: ColosseumStateService) {
    effect(() => {
      const items = this.state.championMarket()?.items ?? [];
      const current = this.selected();
      if (items.length === 0) {
        this.selected.set(null);
        return;
      }

      const refreshed = current
        ? items.find((item) => item.id === current.id)
        : items[0];
      const selected = refreshed ?? items[0];
      this.selected.set(selected);
    });
  }

  select(item: ChampionMarketItem): void {
    this.selected.set(item);
  }

  isSelected(item: ChampionMarketItem): boolean {
    return this.selected()?.id === item.id;
  }

  purchase(item: ChampionMarketItem): void {
    if (!item.canPurchase || this.state.loading()) return;
    this.state.purchaseChampionMarketItem(item.id, 1);
  }

  categoryClass(category: string): string {
    return category === 'Weekly Cache' ? 'll-badge-accent' : 'll-badge-muted';
  }

  itemsForCategory(category: string): ChampionMarketItem[] {
    return (
      this.state
        .championMarket()
        ?.items.filter((item) => item.category === category) ?? []
    );
  }

  rewards(item: ChampionMarketItem): ChampionMarketReward[] {
    const rewards: ChampionMarketReward[] = [];
    if (item.cindersGranted > 0) {
      rewards.push({
        amount: item.cindersGranted.toLocaleString(),
        label: 'Cinders',
      });
    }
    if (item.soulstonesGranted > 0) {
      rewards.push({
        amount: item.soulstonesGranted.toLocaleString(),
        label: 'Soulstones',
      });
    }
    if (item.sigilFragmentsGranted > 0) {
      rewards.push({
        amount: item.sigilFragmentsGranted.toLocaleString(),
        label: 'Sigil Fragments',
      });
    }
    if (item.rewardItemQuantity > 0) {
      rewards.push({
        amount: item.rewardItemQuantity.toLocaleString(),
        label: item.rewardItemName?.trim() || item.rewardItemId || 'Item',
      });
    }

    return rewards.length > 0
      ? rewards
      : [{ amount: 'Unlock', label: item.category }];
  }

  requirementText(item: ChampionMarketItem): string {
    const requirements: string[] = [];
    if (item.requiredRankTier) {
      requirements.push(`${this.titleCase(item.requiredRankTier)} rank`);
    }
    if (item.requiredRating) {
      requirements.push(`${item.requiredRating.toLocaleString()} rating`);
    }
    return requirements.length > 0
      ? requirements.join(' / ')
      : 'No requirement';
  }

  limitProgress(item: ChampionMarketItem): string {
    if (item.weeklyPurchaseLimit != null) {
      return `${item.remainingWeeklyPurchases} / ${item.weeklyPurchaseLimit} left`;
    }
    if (item.lifetimePurchaseLimit != null) {
      return `${item.remainingLifetimePurchases} / ${item.lifetimePurchaseLimit} left`;
    }
    return 'None';
  }

  limitDescription(item: ChampionMarketItem): string {
    if (item.weeklyPurchaseLimit != null) {
      return `${item.remainingWeeklyPurchases} / ${item.weeklyPurchaseLimit} left this week`;
    }
    if (item.lifetimePurchaseLimit != null) {
      return `${item.remainingLifetimePurchases} / ${item.lifetimePurchaseLimit} left lifetime`;
    }
    return 'No purchase limit';
  }

  resetLabel(resetAt: Date | string): string {
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

  private titleCase(value: string): string {
    return value
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, (letter) => letter.toUpperCase());
  }
}
