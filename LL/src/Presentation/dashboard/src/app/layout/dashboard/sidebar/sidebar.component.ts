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
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CharacterActionsService } from '../../../core/services/api/character-actions/character-actions.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [NgFor, NgIf, SidebarItemComponent, RouterLink, TabComponent],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent implements OnInit {
  @Output() itemTapped = new EventEmitter<void>();

  @ViewChild('tabComponent') tabComponent!: TabComponent;
  tabs: Tab[] = [{ label: '', items: [] as SidebarItem[] }];
  activeTab: string = '';
  activeItem: string = '';
  displayCurrentAction$!: Observable<boolean>;

  constructor(
    private sidebarService: SidebarService,
    private gameService: GameService,
    private actionService: CharacterActionsService,
    private router: Router,
  ) {}
  ngOnInit() {
    this.displayCurrentAction$ = this.actionService.displayCurrentAction$;

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
    ); // Navigating to a Navbar item displays the first item in the sidebar. If you simply refresh the page, display the sidebar item you're already on
  }

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
  }
  navigateTo(tabLabel: string) {
    this.activeItem = tabLabel;
    // TODO: Can be optimized. Check whether CombatVisible before sending a new call to hide combat
    this.gameService.hideCombat();
  }

  toggleSidebar() {
    this.itemTapped.emit();
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }
}
