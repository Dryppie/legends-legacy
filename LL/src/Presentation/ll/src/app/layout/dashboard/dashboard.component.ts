import { Component, HostListener, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { InventoryComponent } from '../../features/game/character/inventory/inventory.component';
import { NavbarComponent } from './navbar/navbar.component';
import { MainViewComponent } from './main/main-view.component';
import { CharacterActionsService } from '../../core/services/character-actions/character-actions.service';
import { BackButtonComponent } from '../../shared/components/back-button/back-button.component';
import { NgClass, NgIf } from '@angular/common';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RouterOutlet,
    SidebarComponent,
    InventoryComponent,
    InventoryComponent,
    NavbarComponent,
    MainViewComponent,
    BackButtonComponent,
    NgIf,
    NgClass,
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  isSidebarOpen = true;
  isScreenSmall = false;

  ngOnInit() {
    this.checkScreenSize();
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
}
