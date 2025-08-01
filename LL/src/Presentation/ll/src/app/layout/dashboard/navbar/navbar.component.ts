import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { CharacterBadgeComponent } from '../../../shared/components/character-badge/character-badge.component';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { interval, Subscription } from 'rxjs';
import { PlayerService } from '../../../core/services/api/players/player.service';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';
import { TourService } from '../../../core/services/client-side/tutorial-tour/tour.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [
    CharacterBadgeComponent,
    NavbuttonComponent,
    NgIf,
    NgFor,
    NumberFormatPipe,
    ShortNumberPipe,
  ],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();
  @Input() isScreenSmall!: boolean;
  showList = false;
  activeLabel = 'Character';
  navButtons = [
    { link: '/game/character', label: 'Character', dataTour: '' },
    { link: '/game/city', label: 'City', dataTour: '' },
    { link: '/game/professions', label: 'Professions', dataTour: '' },
    { link: '/game/world', label: 'World', dataTour: 'navigate-to-world' },
    { link: '/game/settings', label: 'Settings', dataTour: '' },
  ];

  useShortFormat = false;

  readonly currentCharacter;
  onlinePlayers: number = 0;

  private onlinePlayersSub?: Subscription;

  constructor(
    private authService: AuthService,
    private readonly playerService: PlayerService,
    private tour: TourService,
  ) {
    this.currentCharacter = this.authService.currentCharacter;
    this.tour.start('combat');
  }

  ngOnInit(): void {
    this.loadOnlinePlayers();

    this.onlinePlayersSub = interval(1000 * 60 * 2).subscribe(() => {
      this.loadOnlinePlayers();
    });

    const stored = localStorage.getItem('useShortFormat');
    this.useShortFormat = stored === 'true';
  }

  ngOnDestroy(): void {
    this.onlinePlayersSub?.unsubscribe();
  }

  toggleFormat() {
    this.useShortFormat = !this.useShortFormat;
    localStorage.setItem('useShortFormat', this.useShortFormat.toString());
  }

  loadOnlinePlayers() {
    this.playerService.getOnlinePlayerCount().subscribe({
      next: (count) => {
        this.onlinePlayers = count;
      },
      error: (err) => console.error('Failed to load online players', err),
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
