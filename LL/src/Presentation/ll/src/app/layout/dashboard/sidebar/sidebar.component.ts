import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { SidebarItem, Tab } from '../../../shared/models/sidebar-item';
import { SidebarService } from '../../../core/services/sidebar/sidebar.service';
import { TabComponent } from '../../../shared/components/tab/tab.component';

// interface SidebarItem {
//   title: string;
//   icon: string;
//   route: string; // If you have routes, add a 'route' property
//   messageCount?: number; // Optional property for items like "Messages"
// }

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    SidebarItemComponent,
    RouterLink,
    RouterLinkActive,
    TabComponent,
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent implements OnInit {
  @Output() itemTapped = new EventEmitter<void>();

  @ViewChild('tabComponent') tabComponent!: TabComponent;
  tabs: Tab[] = [{ label: '', items: [] as SidebarItem[] }];
  activeTab: string = '';
  activeItem: string = '';

  constructor(private sidebarService: SidebarService) {}

  ngOnInit() {
    this.sidebarService.currentContent$.subscribe((link) => {
      this.tabs = [];
      this.updateSidebarItems(link);
    });
  }

  initializeTabs() {
    // You can initialize with hardcoded tabs, or fetch from a service if needed
    this.tabs = [
      {
        label: 'Daily',
        items: [
          {
            id: '1',
            route: 'character/inventory',
            icon: 'path/to/quest-icon.png',
            title: 'Character Overview',
            description: 'Statistics and Equipment',
            rewards: [
              { icon: 'path/to/fire-icon.png', amount: 2 },
              { icon: 'path/to/coin-icon.png', amount: 1 },
            ],
          },
          {
            id: '2',
            route: 'character/inventory',
            icon: 'path/to/quest-icon.png',
            title: 'Inventory',
            description: '60/100',
            rewards: [
              { icon: 'path/to/fire-icon.png', amount: 2 },
              { icon: 'path/to/coin-icon.png', amount: 1 },
            ],
          },
          {
            id: '3',
            route: 'character/essences',
            icon: 'path/to/quest-icon.png',
            title: 'Essences',
            description: 'More details about the quests',
            rewards: [
              { icon: 'path/to/fire-icon.png', amount: 2 },
              { icon: 'path/to/coin-icon.png', amount: 1 },
            ],
          },
          {
            id: '4',
            route: 'character/achievements',
            icon: 'path/to/quest-icon.png',
            title: 'Achievements & Titles',
            description: 'More details about the quests',
            rewards: [
              { icon: 'path/to/fire-icon.png', amount: 2 },
              { icon: 'path/to/coin-icon.png', amount: 1 },
            ],
          },
        ] as SidebarItem[],
      },
      {
        label: 'Weekly',
        items: [
          {
            id: '1',
            route: 'quest/1',
            icon: 'path/to/quest-icon.png',
            title: 'Wolf Hunting',
            description: 'More details about the quests',
            rewards: [
              { icon: 'path/to/fire-icon.png', amount: 3 },
              { icon: 'path/to/coin-icon.png', amount: 2 },
            ],
          },
        ] as SidebarItem[],
      },
    ];
  }

  updateSidebarItems(url: string) {
    // Based on the URL, decide whether to fetch dynamic items or use static ones
    if (url.includes('professions')) {
      this.sidebarService.getSidebarItems().subscribe((SidebarItems) => {
        // Assuming dynamic items need to be appended to a specific tab or replace an existing one
        const sidebarTab = {
          label: 'Gathering',
          items: SidebarItems.map((item) => ({
            id: item.id,
            route: item.route,
            icon: item.icon,
            title: item.title,
            description: item.description,
            rewards: item.rewards,
          })),
        };
        this.tabs.push(sidebarTab);
      });
    } else {
      this.initializeTabs();
    }
    // Logic for updating sidebar based on static content can go here
    this.setActiveTab(this.tabs[0]?.label || '');
    this.navigateTo((this.tabs[0].items[0] as SidebarItem).id || '');
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }
  navigateTo(tabLabel: string) {
    this.activeItem = tabLabel;
  }

  toggleSidebar() {
    this.itemTapped.emit();
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
