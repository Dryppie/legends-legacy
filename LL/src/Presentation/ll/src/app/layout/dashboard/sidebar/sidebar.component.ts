import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  effect,
  EventEmitter,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { Router, RouterLink } from '@angular/router';
import { SidebarItem, Tab } from '../../../shared/models/sidebar-item';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { TabComponent } from '../../../shared/components/tabs/tab/tab.component';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CurrentActionComponent } from '../../../shared/components/current-action/current-action.component';
import { FilterTabsComponent } from '../../../shared/components/tabs/filter-tabs/filter-tabs.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { Equipment } from '../../../shared/models/item';
import { EquipmentType } from '../../../shared/models/enums/equipmentType';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    SidebarItemComponent,
    RouterLink,
    CurrentActionComponent,
    FilterTabsComponent,
    ShortNumberPipe,
    NumberFormatPipe,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit {
  @Output() itemTapped = new EventEmitter<void>();

  @ViewChild('tabComponent') tabComponent!: TabComponent;
  tabs: Tab[] = [{ label: '', items: [] as SidebarItem[] }];
  activeTab: string = '';
  activeItem: string = '';
  displayCurrentAction = false;
  useShortFormat = false;

  readonly currentCharacter;

  constructor(
    private sidebarService: SidebarService,
    private gameService: GameService,
    private state: CharacterActionsStateService,
    private characterState: CharacterStateService,
    private router: Router,
  ) {
    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });

    this.currentCharacter = this.characterState.currentCharacter;
  }

  ngOnInit(): void {
    // Observable: currentContent$ still RxJS based
    this.sidebarService.currentContent$.subscribe((link) => {
      this.tabs = [];
      this.updateSidebar(link);
    });
  }

  toggleFormat() {
    this.useShortFormat = !this.useShortFormat;
    localStorage.setItem('useShortFormat', this.useShortFormat.toString());
  }

  updateSidebar(url: string) {
    this.sidebarService.getSidebar(url).subscribe((Sidebar) => {
      this.tabs = Sidebar;
    });

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
    this.gameService.hideCombat();
  }

  toggleSidebar() {
    this.itemTapped.emit();
  }

  get tabLabels(): string[] {
    return this.tabs.map((tab) => tab.label);
  }

  navigateToAction() {
    const action = this.state.currentAction();
    if (!action) return;
    const actionType = action.characterActionType;
    const now = Date.now();
    const updatedAt = new Date(action.updatedAt ?? 0).getTime();
    if (updatedAt > now) this.gameService.showCombat();
    else {
      let extendedPath = '';
      if (actionType === CharacterActionType.Gathering) {
        extendedPath = 'gathering';
      } else {
        const equipmentType = (
          action.craftingActionDetails?.craftingQueueItems[0].equipmentInstance
            .itemBase as Equipment
        ).equipmentType;
        switch (equipmentType) {
          case EquipmentType.Head:
          case EquipmentType.Chest:
          case EquipmentType.Legs:
            extendedPath = '/crafting/armorforging';
            break;
          case EquipmentType.TwoHanded:
          case EquipmentType.OneHanded:
          case EquipmentType.OffHand:
            extendedPath = '/crafting/weaponsmithing';
            break;
          case EquipmentType.Relic:
          case EquipmentType.Necklace:
          case EquipmentType.Ring:
            extendedPath = '/crafting/jewelrycrafting';
            break;
        }
      }
      this.router.navigate([`game/professions/${extendedPath}`]);
      this.sidebarService.updateContent(`game/professions/${extendedPath}`);
    }
  }
}
