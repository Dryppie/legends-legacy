import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { RouterLink } from '@angular/router';
import { SidebarItem, Tab } from '../../../shared/models/sidebar-item';
import { SidebarService } from '../../../core/services/sidebar/sidebar.service';
import { TabComponent } from '../../../shared/components/tab/tab.component';
import { GameService } from '../../../core/services/game/game.service';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CharacterActionDto } from '../../../shared/models/Dtos/characterActionDto';
import { CurrentActionComponent } from '../../../shared/components/current-action/current-action.component';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    SidebarItemComponent,
    RouterLink,
    TabComponent,
    CurrentActionComponent,
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent implements OnInit {
  navigateToCombat() {
    this.gameService.showCombat();
  }
  @Output() itemTapped = new EventEmitter<void>();

  @ViewChild('tabComponent') tabComponent!: TabComponent;
  tabs: Tab[] = [{ label: '', items: [] as SidebarItem[] }];
  activeTab: string = '';
  activeItem: string = '';
  currentAction: CharacterActionDto | null = {} as CharacterActionDto;

  constructor(
    private sidebarService: SidebarService,
    private gameService: GameService,
    private actionService: CharacterActionsService,
  ) {}

  ngOnInit() {
    this.actionService.currentAction$.subscribe((action) => {
      this.currentAction = action;
    });

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
