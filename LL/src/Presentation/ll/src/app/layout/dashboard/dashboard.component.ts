import { Component, HostListener, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Observable } from 'rxjs';
import { GameService } from '../../core/services/client-side/game/game.service';
import { CombatComponent } from '../../shared/components/combat/combat.component';
import { ChatComponent } from './chat/chat.component';
import { LootTrackerComponent } from './loot-tracker/loot-tracker.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    NavbarComponent,
    NgIf,
    NgClass,
    AsyncPipe,
    CombatComponent,
    ChatComponent,
    LootTrackerComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = false;
  isScreenSmall = false;
  isScreenLarge = false;
  isChatOpenDesktop = true; // open by default on ≥ lg
  isFloatingChatOpen = false;
  combatVisible$!: Observable<boolean>;

  constructor(private gameService: GameService) {}

  ngOnInit() {
    this.checkScreenSize();
    this.combatVisible$ = this.gameService.combatVisible$;
  }

  @HostListener('window:resize', ['$event'])
  onResize() {
    this.checkScreenSize();
  }

  checkScreenSize() {
    const nextIsScreenSmall = window.innerWidth < 640;

    if (nextIsScreenSmall && !this.isScreenSmall) {
      this.isSidebarOpen = false;
    }

    if (!nextIsScreenSmall) {
      this.isSidebarOpen = true;
    }

    this.isScreenSmall = nextIsScreenSmall;
    this.isScreenLarge = window.innerWidth >= 1280;
  }

  toggleNav() {
    if (!this.isScreenSmall) return;

    this.isSidebarOpen = !this.isSidebarOpen;
  }

  toggleChat(): void {
    this.isScreenLarge
      ? (this.isChatOpenDesktop = !this.isChatOpenDesktop)
      : (this.isFloatingChatOpen = !this.isFloatingChatOpen);
  }

  openSidebar() {
    this.isSidebarOpen = true;
  }

  closeSidebar() {
    if (this.isScreenSmall) {
      this.isSidebarOpen = false;
    }
  }
}
