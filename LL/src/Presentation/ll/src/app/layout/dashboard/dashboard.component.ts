import { Component, OnInit } from '@angular/core';
import { SidebarComponent } from './sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';
import { InventoryComponent } from '../../features/game/character/inventory/inventory.component';
import { NavbarComponent } from './navbar/navbar.component';
import { MainViewComponent } from './main/main-view.component';
import { CharacterActionsService } from '../../core/services/character-actions/character-actions.service';

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
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css',
})
export class DashboardComponent implements OnInit {
  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    // this.characterActionsService.getCharacterAction().subscribe();
  }
}
