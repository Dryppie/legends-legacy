import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { GuildShopItem } from '../../../../../../shared/models/Dtos/guild/guildShop';
import { GuildShopComponent } from './guild-shop.component';

describe('GuildShopComponent purchase limits', () => {
  it('uses remaining purchases in both the list and item details', () => {
    const component = TestBed.runInInjectionContext(
      () =>
        new GuildShopComponent({
          shop: signal(null),
          loading: signal(false),
        } as unknown as GuildStateService),
    );
    const item = {
      weeklyLimit: 2,
      purchasedThisPeriod: 0,
    } as GuildShopItem;

    expect(component.limitProgress(item)).toBe('2 / 2 left');
    expect(component.limitDescription(item)).toBe('2 / 2 left this week');
  });

  it('describes only the Market Office stock requirement', () => {
    const component = TestBed.runInInjectionContext(
      () =>
        new GuildShopComponent({
          shop: signal(null),
          loading: signal(false),
        } as unknown as GuildStateService),
    );
    const item = { requiredMarketOfficeLevel: 4 } as GuildShopItem;

    expect(component.requirementText(item)).toBe('Market Office 4');
  });
});
