import { Component, OnInit } from '@angular/core';
import { BannerComponent } from '../../../../shared/components/banner/banner.component';
import { TabComponent } from '../../../../shared/components/tab/tab.component';
import { Tab } from '../../../../shared/models/sidebar-item';

@Component({
  selector: 'app-market-place',
  standalone: true,
  imports: [BannerComponent, TabComponent],
  templateUrl: './market-place.component.html',
  styleUrl: './market-place.component.css',
})
export class MarketPlaceComponent implements OnInit {
  ngOnInit(): void {
    this.setActiveTab(this.tabs[0]?.label || '');
  }

  tabs: Tab[] = [
    {
      label: 'Buy',
      items: [],
    },
    // {
    //   label: 'Tournament Grounds',
    //   items: [],
    // },
    // {
    //   label: `Champion's Market`,
    //   items: [],
    // },
    {
      label: 'Your Orders',
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
