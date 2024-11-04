import { Component, Input } from '@angular/core';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/auth/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CharacterBadgeComponent,
    RouterLink,
    RouterLinkActive,
    NavbuttonComponent,
    NgIf,
    NgFor,
  ],
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'], // Corrected to `styleUrls`
})
export class NavbarComponent {
  logout() {
    this.authService.logout();
  }
  constructor(private authService: AuthService) {}
  @Input() isScreenSmall!: boolean;
  showList = false;
  activeLabel = 'Character'; // Default active label
  navButtons = [
    { link: '/game/character', label: 'Character' },
    { link: '/game/professions', label: 'Professions' },
    { link: '/game/world', label: 'World' },
    // { link: '#', label: 'Team' },
    // { link: '#', label: 'Town' },
  ];

  toggleList() {
    this.showList = !this.showList;
  }

  activeNavbar(activeLabel: string) {
    this.activeLabel = activeLabel;
  }
}
