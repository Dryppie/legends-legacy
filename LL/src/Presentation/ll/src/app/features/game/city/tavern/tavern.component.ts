import { NgClass, NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { LeaderboardStateService } from '../../../../core/services/api/leaderboard/leaderboard-state.service';
import {
  DropdownComponent,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import {
  LeaderboardBoard,
  LeaderboardBoardEntry,
} from '../../../../shared/models/Dtos/leaderboard/leaderboard';
import { NumberFormatPipe } from '../../../../shared/pipes/number-format/number-format.pipe';
import { CharacterTagComponent } from '../../../../shared/components/character/character-tag/character-tag.component';
import { environment } from '../../../../../environments/environment';
import { LocalDatePipe } from '../../../../shared/pipes/local-date/local-date.pipe';

type LeaderboardCategory = 'Overall' | 'PvE' | 'PvP' | 'Guilds';

interface BoardOption {
  key: string;
  label: string;
  category: LeaderboardCategory;
}

@Component({
  selector: 'app-tavern',
  imports: [
    LocalDatePipe,
    DefaultHeaderComponent,
    DropdownComponent,
    NgClass,
    NgFor,
    NgIf,
    NumberFormatPipe,
    CharacterTagComponent,
  ],
  templateUrl: './tavern.component.html',
})
export class TavernComponent implements OnInit {
  readonly podiumOrder = [0, 1, 2];
  private readonly allCategories: readonly LeaderboardCategory[] = [
    'Overall',
    'PvE',
    'PvP',
    'Guilds',
  ];
  private readonly allBoards: BoardOption[] = [
    { key: 'combat-level', label: 'Combat Level', category: 'Overall' },
    {
      key: 'soul-archive-completion',
      label: 'Soul Archive',
      category: 'Overall',
    },
    {
      key: 'achievement-renown',
      label: 'Achievement Renown',
      category: 'Overall',
    },
    { key: 'dungeon-mastery', label: 'Dungeon Mastery', category: 'PvE' },
    {
      key: 'most-dungeon-clears',
      label: 'Most Dungeon Clears',
      category: 'PvE',
    },
    ...(environment.features.raids
      ? [
          {
            key: 'raid-boss-kills',
            label: 'Raid Boss Kills',
            category: 'PvE' as const,
          },
          {
            key: 'fastest-raid-slain.raid-boss.hives-abyss',
            label: "Fastest Hive's Abyss",
            category: 'PvE' as const,
          },
          {
            key: 'fastest-raid-slain.raid-boss.sanguine-horror',
            label: 'Fastest Sanguine Horror',
            category: 'PvE' as const,
          },
        ]
      : []),
    { key: 'arena-rating', label: 'Arena Rating', category: 'PvP' },
    { key: 'tournament-points', label: 'Tournament Points', category: 'PvP' },
    {
      key: 'weekly-guild-contribution',
      label: 'Weekly Contribution',
      category: 'Guilds',
    },
    { key: 'guild-renown', label: 'Guild Renown', category: 'Guilds' },
  ];

  activeCategory: LeaderboardCategory = 'Overall';
  activeBoardKey = 'combat-level';

  constructor(readonly state: LeaderboardStateService) {}

  ngOnInit(): void {
    this.state.reset();
    this.state.load(this.activeBoardKey);
  }

  get categories(): readonly LeaderboardCategory[] { return this.allCategories; }

  get boards(): BoardOption[] { return this.allBoards; }

  get categoryBoards(): BoardOption[] {
    return this.boards.filter(
      (board) => board.category === this.activeCategory,
    );
  }

  boardLabels(category: LeaderboardCategory): readonly string[] {
    return this.boards
      .filter((board) => board.category === category)
      .map((board) => board.label);
  }

  selectLeaderboard(selection: DropdownSelection<LeaderboardCategory>): void {
    const board = this.boards.find(
      (candidate) =>
        candidate.category === selection.main &&
        candidate.label === selection.sub,
    );
    if (!board) return;

    this.activeCategory = selection.main;
    this.selectBoard(board.key);
  }

  selectCategory(categoryKey: string): void {
    const category = this.categories.find(
      (candidate) => candidate === categoryKey,
    );
    if (!category) return;
    if (category === this.activeCategory) return;
    this.activeCategory = category;
    const firstBoard = this.categoryBoards[0];
    if (firstBoard) this.selectBoard(firstBoard.key);
  }

  selectBoard(boardKey: string): void {
    if (
      boardKey === this.activeBoardKey ||
      !this.boards.some((board) => board.key === boardKey)
    )
      return;
    this.activeBoardKey = boardKey;
    this.state.load(boardKey);
  }

  refresh(): void {
    this.state.refresh();
  }

  jumpToParticipant(event: Event, query: string): void {
    event.preventDefault();
    this.state.jumpToParticipant(query);
  }

  clearJump(): void {
    this.state.clearJump();
  }

  loadPage(cursor: string | null): void {
    if (cursor) this.state.loadPage(cursor);
  }

  isViewer(entryId: string): boolean {
    return entryId === this.state.board()?.viewerEntry?.participantId;
  }

  isSearchMatch(entryId: string): boolean {
    return entryId === this.state.board()?.searchMatch?.participantId;
  }

  showPodium(board: LeaderboardBoard): boolean {
    return board.pageStartRank === 1;
  }

  isCharacterBoard(board: LeaderboardBoard): boolean {
    return board.participantLabel !== 'Guild';
  }

  tableEntries(board: LeaderboardBoard): LeaderboardBoardEntry[] {
    return board.entries.slice(this.showPodium(board) ? 3 : 0);
  }

  trackByParticipantId = (_: number, entry: { participantId: string }) =>
    entry.participantId;
}
