import { Component, HostListener, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { InventoryComponent } from '../../features/game/character/inventory/inventory.component';
import { NavbarComponent } from './navbar/navbar.component';
import { BackButtonComponent } from '../../shared/components/back-button/back-button.component';
import { AsyncPipe, NgClass, NgIf } from '@angular/common';
import { Observable } from 'rxjs';
import { GameService } from '../../core/services/game/game.service';
import { CombatComponent } from '../../shared/components/combat/combat.component';

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
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = true;
  isScreenSmall = false;
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
  }

  toggleNav() {
    this.isSidebarOpen = !this.isSidebarOpen;
  }

  openSidebar() {
    this.isSidebarOpen = true;
  }
}
