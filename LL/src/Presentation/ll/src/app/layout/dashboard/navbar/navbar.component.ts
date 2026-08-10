import {
  Component,
  effect,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { NavbuttonComponent } from './navbutton/navbutton.component';
import { NgFor, NgIf } from '@angular/common';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { interval, Subscription } from 'rxjs';
import { PlayerService } from '../../../core/services/api/players/player.service';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { Router } from '@angular/router';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';

@Component({
  selector: 'app-navbar',
  imports: [NavbuttonComponent, NgIf, NgFor, NumberFormatPipe, ShortNumberPipe],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();
  @Output() chatTapped = new EventEmitter<void>();
  @Input() isScreenSmall!: boolean;
  @Input() showChatButton = true;
  showList = false;
  activeLabel = 'Character';
  navButtons = [
    {
      link: '/game/character/character-overview',
      label: 'Character',
      icon: 'icons/character/achievements.svg',
    },
    {
      link: '/game/world/shenic',
      label: 'World',
      icon: 'icons/world/Quest.svg',
    },
    {
      link: '/game/city/tavern',
      label: 'Leaderboard',
      icon: 'icons/podium/Wreath.svg',
    },
    {
      link: '/game/settings',
      label: 'Settings',
      icon: 'icons/settings/settings.svg',
    },
  ];

  displayCurrentAction = false;

  useShortFormat = false;

  readonly currentCharacter;
  onlinePlayers: number = 0;

  private onlinePlayersSub?: Subscription;

  constructor(
    private authService: AuthService,
    private readonly playerService: PlayerService,
    private readonly state: CharacterActionsStateService,
    private readonly router: Router,
  ) {
    this.currentCharacter = this.authService.currentCharacter;

    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });
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
  }

  navigateToAction(): void {
    const action = this.state.currentAction();
    if (!action) return;

    const actionType = action.characterActionType;

    if (actionType === CharacterActionType.Combat) {
      this.router.navigate(['/game/combat']);
      return;
    }

    if (actionType === CharacterActionType.Crafting) {
      this.router.navigate(['/game/professions/crafting'], {
        queryParams: { tab: 'tempering' },
      });
    } else {
      return;
    }
  }

  logout() {
    this.authService.logout();
  }
}
