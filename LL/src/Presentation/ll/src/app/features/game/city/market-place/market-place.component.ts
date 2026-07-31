import { Component, effect, OnInit, signal } from '@angular/core';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';
import { MarketPlaceFilterComponent } from '../../../../shared/components/market-place/market-place-filter/market-place-filter.component';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { NgIf, NgSwitch, NgSwitchCase } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MarketplaceStateService } from '../../../../core/services/api/market-place/market-place-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { AuthService } from '../../../../core/services/api/auth/auth.service';
import { UserInfoDto } from '../../../../shared/models/Dtos/userInfoDto';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';
import { MarketPlaceCommodityComponent } from './market-place-commodity/market-place-commodity.component';
import { MarketPlaceOrdersComponent } from './market-place-orders/market-place-orders.component';
import { MarketCategorySelection } from '../../../../shared/models/market-category';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';

type MarketPlaceMode = 'browse' | 'sell' | 'orders';

@Component({
    selector: 'app-market-place',
    imports: [
        MarketPlaceBuyComponent,
        MarketPlaceSellComponent,
        MarketPlaceFilterComponent,
        MarketPlaceCommodityComponent,
        MarketPlaceOrdersComponent,
        NumberFormatPipe,
        NgIf,
        NgSwitch,
        NgSwitchCase,
        DefaultHeaderComponent,
        RouterLink,
    ],
    templateUrl: './market-place.component.html',
    styleUrl: './market-place.component.css'
})
export class MarketPlaceComponent implements OnInit {
  readonly ItemType = ItemType;
  readonly mode = signal<MarketPlaceMode>('browse');
  readonly mobileDetailOpen = signal(false);
  readonly selectedMarket = signal<MarketCategorySelection>({
    id: 'resources',
    label: 'Resources',
    itemType: ItemType.Resource,
    subcategory: 'Ore',
  });
  userInfo: UserInfoDto | null = null;
  userInfoLoaded = false;
  marketplaceAccessFailed = false;

  constructor(
    readonly marketplaceState: MarketplaceStateService,
    readonly characterState: CharacterStateService,
    private readonly authService: AuthService,
  ) {
    effect(() => {
      const userInfo = this.authService.userInfo();
      if (!userInfo) return;

      this.applyUserInfo(userInfo);
    });
  }

  get canUseMarketplace(): boolean {
    return (
      this.userInfoLoaded &&
      !this.marketplaceAccessFailed &&
      this.userInfo?.isRegisteredUser === true
    );
  }

  get isGuestAccount(): boolean {
    return (
      this.userInfoLoaded &&
      !this.marketplaceAccessFailed &&
      this.userInfo?.isRegisteredUser === false
    );
  }

  ngOnInit(): void {
    this.authService.getUserInfo().subscribe({
      next: (userInfo) => this.applyUserInfo(userInfo),
      error: (err) => {
        this.userInfoLoaded = true;
        this.marketplaceAccessFailed = true;
        console.warn('Unable to load user info for Marketplace access.', err);
      },
    });
  }

  onCategoryChanged(category: MarketCategorySelection): void {
    this.selectedMarket.set(category);
    this.mobileDetailOpen.set(false);
  }

  setMode(mode: MarketPlaceMode): void {
    this.mode.set(mode);
    this.mobileDetailOpen.set(false);
  }

  onMobileDetailChanged(open: boolean): void {
    this.mobileDetailOpen.set(open);
  }

  private applyUserInfo(userInfo: UserInfoDto): void {
    this.userInfo = userInfo;
    this.userInfoLoaded = true;
    this.marketplaceAccessFailed = false;
  }
}
