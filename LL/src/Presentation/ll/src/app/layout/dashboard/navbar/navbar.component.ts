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
import { interval } from 'rxjs';
import { PlayerService } from '../../../core/services/api/players/player.service';

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
  onlinePlayers: number = 2;

  constructor(
    private authService: AuthService,
    private readonly playerService: PlayerService,
  ) {
    this.currentCharacter = this.authService.currentCharacter;
    this.loadOnlinePlayers();
  }

  loadOnlinePlayers() {
    this.playerService.getOnlinePlayerCount().subscribe({
      next: (count) => {
        this.onlinePlayers = count;
      },
      error: (err) => console.error('Failed to load online players', err),
    });
  }
  ngOnInit(): void {
    this.loadOnlinePlayers();

    interval(1000 * 60 * 2).subscribe(() => {
      this.loadOnlinePlayers();
    });
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
