import { NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import { combineLatest, Subject, takeUntil } from 'rxjs';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { LocalStorageService } from '../../../core/services/client-side/local-storage/local-storage.service';
import { SidebarService } from '../../../core/services/client-side/sidebar/sidebar.service';
import { CurrentDungeonComponent } from '../../../shared/components/current-dungeon/current-dungeon.component';
import { SidebarSection, Tab } from '../../../shared/models/sidebar-item';
import { NumberFormatPipe } from '../../../shared/pipes/number-format/number-format.pipe';
import { ShortNumberPipe } from '../../../shared/pipes/number-format/short-number.pipe';
import { TutorialQuestComponent } from '../tutorial-quest/tutorial-quest.component';

@Component({
  selector: 'app-game-header',
  imports: [
    NgIf,
    CurrentDungeonComponent,
    NumberFormatPipe,
    ShortNumberPipe,
    TutorialQuestComponent,
  ],
  templateUrl: './game-header.component.html',
})
export class GameHeaderComponent implements OnInit, OnDestroy {
  @Output() menuTapped = new EventEmitter<void>();

  readonly currentCharacter;
  useShortFormat: boolean;
  activePageTitle = 'Game';
  activeSectionLabel = '';
  private readonly destroy$ = new Subject<void>();

  constructor(
    characterState: CharacterStateService,
    private readonly storage: LocalStorageService,
    private readonly sidebarService: SidebarService,
  ) {
    this.currentCharacter = characterState.currentCharacter;
    this.useShortFormat = this.storage.get<boolean>('useShortFormat') ?? false;
  }

  ngOnInit(): void {
    combineLatest([
      this.sidebarService.getSidebar(),
      this.sidebarService.activeUrl$,
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([sections, activeUrl]) => {
        const activeDestination = this.findActiveDestination(
          sections,
          activeUrl,
        );
        this.activePageTitle = activeDestination?.item.title ?? 'Game';
        this.activeSectionLabel = activeDestination?.section.label ?? '';
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  toggleFormat(): void {
    this.useShortFormat = !this.useShortFormat;
    this.storage.set('useShortFormat', this.useShortFormat);
  }

  private findActiveDestination(
    sections: SidebarSection[],
    activeUrl: string,
  ): { section: SidebarSection; item: Tab } | undefined {
    const path = activeUrl.split('?')[0].split('#')[0];

    for (const section of sections) {
      for (const item of section.items) {
        const itemPath = `/game/${item.route.join('/')}`;
        if (path === itemPath || path.startsWith(`${itemPath}/`)) {
          return { section, item };
        }
      }
    }

    return undefined;
  }
}
