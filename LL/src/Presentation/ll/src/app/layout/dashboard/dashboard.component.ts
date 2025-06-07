import { Component, HostListener, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { NavbarComponent } from './navbar/navbar.component';
import { BackButtonComponent } from '../../shared/components/back-button/back-button.component';
import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Observable } from 'rxjs';
import { GameService } from '../../core/services/client-side/game/game.service';
import { CombatComponent } from '../../shared/components/combat/combat.component';
import { ChatComponent } from './chat/chat.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    NavbarComponent,
    BackButtonComponent,
    NgIf,
    AsyncPipe,
    NgClass,
    CombatComponent,
    ChatComponent,
  ],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = true;
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
    this.isScreenSmall = window.innerWidth < 640;
    this.isScreenLarge = window.innerWidth >= 1280;
  }

  toggleNav() {
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
}
