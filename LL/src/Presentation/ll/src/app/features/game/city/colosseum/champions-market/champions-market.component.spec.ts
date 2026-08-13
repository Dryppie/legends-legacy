import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ColosseumStateService } from '../../../../../core/services/api/colosseum/colosseum-state.service';
import { ChampionMarketItem } from '../../../../../shared/models/Dtos/colosseum/championMarket';
import { ChampionsMarketComponent } from './champions-market.component';

describe('ChampionsMarketComponent purchase limits', () => {
  it('uses remaining purchases in both the list and item details', () => {
    const component = TestBed.runInInjectionContext(
      () =>
        new ChampionsMarketComponent({
          championMarket: signal(null),
          status: signal(null),
        } as unknown as ColosseumStateService),
    );
    const item = {
      weeklyPurchaseLimit: 2,
      remainingWeeklyPurchases: 0,
      lifetimePurchaseLimit: null,
      remainingLifetimePurchases: 0,
    } as ChampionMarketItem;

    expect(component.limitProgress(item)).toBe('0 / 2 left');
    expect(component.limitDescription(item)).toBe('0 / 2 left this week');
  });
});
