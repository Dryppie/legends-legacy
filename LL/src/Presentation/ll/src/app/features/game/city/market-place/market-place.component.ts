import { Component, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';
import { NgSwitch, NgSwitchCase } from '@angular/common';
import { MarketPlaceBuyComponent } from './market-place-buy/market-place-buy.component';
import { MarketPlaceSellComponent } from './market-place-sell/market-place-sell.component';

@Component({
  selector: 'app-market-place',
  standalone: true,
  imports: [
    BannerComponent,
    TabComponent,
    NgSwitch,
    NgSwitchCase,
    MarketPlaceBuyComponent,
    MarketPlaceSellComponent,
  ],
  templateUrl: './market-place.component.html',
  styleUrl: './market-place.component.css',
})
export class MarketPlaceComponent implements OnInit {
  ngOnInit(): void {
    this.setActiveTab(this.tabs[1]?.label || '');
  }

  tabs: Tab[] = [
    {
      label: 'Buy',
      items: [],
    },
    {
      label: 'Sell',
      items: [],
    },
  ];
  activeTab: string = '';

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
