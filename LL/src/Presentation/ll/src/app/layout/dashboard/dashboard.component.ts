import { Component, effect, HostListener, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { Router, RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { AsyncPipe, NgIf } from '@angular/common';
import { Observable } from 'rxjs';
import { GameService } from '../../core/services/client-side/game/game.service';
import { CombatComponent } from '../../shared/components/combat/combat.component';
import { ChatComponent } from './chat/chat.component';
import { LootTrackerComponent } from './loot-tracker/loot-tracker.component';
import { CurrentActionComponent } from '../../shared/components/current-action/current-action.component';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';
import { Equipment } from '../../shared/models/item';
import { EquipmentType } from '../../shared/models/enums/equipmentType';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    NavbarComponent,
    NgIf,
    AsyncPipe,
    CombatComponent,
    ChatComponent,
    LootTrackerComponent,
    CurrentActionComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = false;
  isScreenSmall = false;
  isScreenLarge = false;
  isChatOpenDesktop = true; // open by default on ≥ lg
  isFloatingChatOpen = false;
  displayCurrentAction = false;
  combatVisible$!: Observable<boolean>;

  constructor(
    private readonly gameService: GameService,
    private readonly state: CharacterActionsStateService,
    private readonly router: Router,
  ) {
    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });
  }

  ngOnInit() {
    this.checkScreenSize();
    this.combatVisible$ = this.gameService.combatVisible$;
  }

  @HostListener('window:resize')
  onResize() {
    this.checkScreenSize();
  }

  checkScreenSize() {
    const nextIsScreenSmall = window.innerWidth < 640;
    const nextIsScreenLarge = window.innerWidth >= 1280;

    if (nextIsScreenSmall && !this.isScreenSmall) {
      this.isSidebarOpen = false;
    }

    if (!nextIsScreenSmall) {
      this.isSidebarOpen = true;
    }

    this.isScreenSmall = nextIsScreenSmall;
    this.isScreenLarge = nextIsScreenLarge;

    if (nextIsScreenLarge) {
      this.isFloatingChatOpen = false;
    }
  }

  toggleNav() {
    if (!this.isScreenSmall) return;

    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleChat(): void {
    if (this.isScreenLarge) {
      this.isChatOpenDesktop = !this.isChatOpenDesktop;
      return;
    }

    this.closeSidebar();
    this.isFloatingChatOpen = !this.isFloatingChatOpen;
  }

  openSidebar() {
    this.isFloatingChatOpen = false;
    this.isSidebarOpen = true;
  }

  closeSidebar() {
    if (this.isScreenSmall) {
      this.isSidebarOpen = false;
    }
  }

  navigateToAction(): void {
    const action = this.state.currentAction();
    if (!action) return;

    const actionType = action.characterActionType;
    const now = Date.now();
    const updatedAt = new Date(action.updatedAt ?? 0).getTime();

    if (updatedAt > now) {
      this.gameService.showCombat();
      return;
    }

    let route: string[] = [];

    if (actionType === CharacterActionType.Crafting) {
      const equipmentType = (
        action.craftingActionDetails?.craftingQueueItems[0].equipmentInstance
          .itemBase as Equipment
      ).equipmentType;

      switch (equipmentType) {
        case EquipmentType.Head:
        case EquipmentType.Chest:
        case EquipmentType.Legs:
          route = ['game', 'professions', 'crafting', 'armorforging'];
          break;

        case EquipmentType.TwoHanded:
        case EquipmentType.OneHanded:
        case EquipmentType.OffHand:
          route = ['game', 'professions', 'crafting', 'weaponsmithing'];
          break;

        case EquipmentType.Relic:
        case EquipmentType.Necklace:
        case EquipmentType.Ring:
          route = ['game', 'professions', 'crafting', 'jewelrycrafting'];
          break;

        default:
          return;
      }
    } else {
      return;
    }

    this.router.navigate(route);
  }
}
