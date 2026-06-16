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
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { Equipment } from '../../../shared/models/item';
import { EquipmentType } from '../../../shared/models/enums/equipmentType';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [NavbuttonComponent, NgIf, NgFor, NumberFormatPipe, ShortNumberPipe],
  templateUrl: './navbar.component.html',
})
export class NavbarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();
  @Output() chatTapped = new EventEmitter<void>();
  @Input() isScreenSmall!: boolean;
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
      icon: 'icons/ui/Leaderboard.svg',
    },
    {
      link: '/game/settings',
      label: 'Settings',
      icon: 'icons/ui/Settings.svg',
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
    private readonly gameService: GameService,
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
    const now = Date.now();
    const updatedAt = new Date(action.updatedAt ?? 0).getTime();

    if (updatedAt > now) {
      this.gameService.showCombat();
      return;
    }

    let route: string[] = [];

    if (actionType === CharacterActionType.Gathering) {
      route = [
        'game',
        'professions',
        'gathering',
        action.gatheringActionDetails!.professionType.toLowerCase(),
      ];
    } else {
      const equipmentType = (
        action.craftingActionDetails?.craftingQueueItems[0].equipmentInstance
          .itemBase as Equipment
      ).equipmentType;

      switch (equipmentType) {
        case EquipmentType.Head:
        case EquipmentType.Chest:
        case EquipmentType.Legs:
          route = ['game', 'professions', 'crafting', 'armorforging'];
          break;

        case EquipmentType.TwoHanded:
        case EquipmentType.OneHanded:
        case EquipmentType.OffHand:
          route = ['game', 'professions', 'crafting', 'weaponsmithing'];
          break;

        case EquipmentType.Relic:
        case EquipmentType.Necklace:
        case EquipmentType.Ring:
          route = ['game', 'professions', 'crafting', 'jewelrycrafting'];
          break;

        default:
          return;
      }
    }

    this.router.navigate(route);
  }

  logout() {
    this.authService.logout();
  }
}
