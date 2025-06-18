import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { Router, RouterLink } from '@angular/router';
import { SidebarItem, Tab } from '../../../shared/models/sidebar-item';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { TabComponent } from '../../../shared/components/tab/tab.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [NgFor, NgIf, SidebarItemComponent, RouterLink, TabComponent],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit {
  @Output() itemTapped = new EventEmitter<void>();

  @ViewChild('tabComponent') tabComponent!: TabComponent;
  tabs: Tab[] = [{ label: '', items: [] as SidebarItem[] }];
  activeTab: string = '';
  activeItem: string = '';

  constructor(
    private sidebarService: SidebarService,
    private router: Router,
  ) {}
  ngOnInit() {
    this.sidebarService.currentContent$.subscribe((link) => {
      this.tabs = [];
      this.updateSidebar(link);
    });
  }

  updateSidebar(url: string) {
    this.tabs = this.sidebarService.getSidebar();

    const activeSidebarItem = this.tabs
      .flatMap((tab) => tab.items.map((item) => item))
      .filter((item) => url.includes(item.id));

    this.setActiveTab(this.tabs[0]?.label || '');
    this.navigateTo(
      (activeSidebarItem[0] as SidebarItem)?.id ||
        (this.tabs[0].items[0] as SidebarItem).id ||
        '',
    );
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
