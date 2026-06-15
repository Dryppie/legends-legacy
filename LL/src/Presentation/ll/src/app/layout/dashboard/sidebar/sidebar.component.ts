import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  effect,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { Router, RouterLink, NavigationEnd } from '@angular/router';
import { filter, Subject, takeUntil } from 'rxjs';
import { SidebarItemComponent } from './sidebar-item/sidebar-item.component';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { GameService } from '../../../core/services/client-side/game/game.service';
import { CurrentActionComponent } from '../../../shared/components/current-action/current-action.component';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { CharacterActionType } from '../../../shared/models/enums/characterActionType';
import { Equipment } from '../../../shared/models/item';
import { EquipmentType } from '../../../shared/models/enums/equipmentType';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { SidebarSection } from '../../../shared/models/sidebar-item';
import { CurrentDungeonComponent } from '../../../shared/components/current-dungeon/current-dungeon.component';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { ColosseumStateService } from '../../../core/services/api/colosseum/colosseum-state.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    SidebarItemComponent,
    RouterLink,
    CurrentActionComponent,
    ShortNumberPipe,
    NumberFormatPipe,
    CurrentDungeonComponent,
  ],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Output() itemTapped = new EventEmitter<void>();

  private readonly destroy$ = new Subject<void>();

  sections: SidebarSection[] = [];
  activeUrl = '';
  displayCurrentAction = false;
  useShortFormat = false;

  readonly currentCharacter;
  readonly guildNotificationCount;
  readonly colosseumNotificationCount;

  constructor(
    private readonly sidebarService: SidebarService,
    private readonly gameService: GameService,
    private readonly state: CharacterActionsStateService,
    private readonly characterState: CharacterStateService,
    private readonly guildState: GuildStateService,
    private readonly colosseumState: ColosseumStateService,
    private readonly router: Router,
  ) {
    effect(() => {
      this.displayCurrentAction = this.state.displayCurrentAction();
    });

    this.currentCharacter = this.characterState.currentCharacter;
    this.guildNotificationCount = this.guildState.guildNotificationCount;
    this.colosseumNotificationCount = this.colosseumState.notificationCount;
  }

  ngOnInit(): void {
    const savedFormat = localStorage.getItem('useShortFormat');
    this.useShortFormat = savedFormat === 'true';

    this.activeUrl = this.router.url;

    this.sidebarService
      .getSidebar()
      .pipe(takeUntil(this.destroy$))
      .subscribe((sections) => {
        this.sections = sections;
      });

    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
        takeUntil(this.destroy$),
      )
      .subscribe((event) => {
        this.activeUrl = event.urlAfterRedirects;
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleFormat(): void {
    this.useShortFormat = !this.useShortFormat;
    localStorage.setItem('useShortFormat', this.useShortFormat.toString());
  }

  toggleSidebar(): void {
    this.itemTapped.emit();
  }

  onNavigate(): void {
    this.gameService.hideCombat();
    this.itemTapped.emit();
  }

  isItemActive(route: string[], exact = false): boolean {
    return this.sidebarService.isRouteActive(route, exact);
  }

  trackSection(index: number, section: SidebarSection): string {
    return section.id;
  }

  trackItem(index: number, item: { id: string }): string {
    return item.id;
  }

  getNotificationCount(itemId: string): number {
    switch (itemId) {
      case 'guild':
        return this.guildNotificationCount();
      case 'colosseum':
        return this.colosseumNotificationCount();
      default:
        return 0;
    }
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
}
