import { Component, ElementRef, ViewChild, OnInit, Input } from '@angular/core';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';

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
  @Input() isScreenSmall!: boolean;
  showList = false;
  activeLabel = 'Character'; // Default active label
  navButtons = [
    { link: '/game/character', label: 'Character' },
    { link: '/game/professions', label: 'Professions' },
    { link: '#', label: 'Team' },
    { link: '#', label: 'Town' },
  ];

  toggleList() {
    this.showList = !this.showList;
  }

  activeNavbar(activeLabel: string) {
    this.activeLabel = activeLabel;
  }
}
