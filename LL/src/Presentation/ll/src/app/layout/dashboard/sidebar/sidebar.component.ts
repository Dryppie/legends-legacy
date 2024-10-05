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
      this.updateSidebar(link);
    });
  }

  updateSidebar(url: string) {
    this.sidebarService.getSidebar(url).subscribe((Sidebar) => {
      this.tabs = Sidebar;
    });

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
