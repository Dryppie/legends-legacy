import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  Signal,
} from '@angular/core';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterDto } from '../../../shared/models/Dtos/characterDto';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CharacterBadgeComponent, NavbuttonComponent, NgIf, NgFor],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent {
  @Output() itemTapped = new EventEmitter<void>();
  @Input() isScreenSmall!: boolean;
  showList = false;
  activeLabel = 'Character'; // Default active label
  navButtons = [
    { link: '/game/character', label: 'Character' },
    { link: '/game/city', label: 'City' },
    { link: '/game/professions', label: 'Professions' },
    { link: '/game/world', label: 'World' },
    // { link: '#', label: 'Town' },
    { link: '/game/settings', label: 'Settings' },
  ];

  readonly currentCharacter;

  constructor(private authService: AuthService) {
    this.currentCharacter = this.authService.currentCharacter;
  }

  toggleList() {
    this.showList = !this.showList;
  }

  activeNavbar(activeLabel: string) {
    this.activeLabel = activeLabel;
    this.itemTapped.emit();
  }

  logout() {
    this.authService.logout();
  }
}
