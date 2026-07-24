import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
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

type LeaderboardCategory = 'Overall' | 'PvE' | 'PvP' | 'Professions' | 'Guilds';

interface BoardOption {
  key: string;
  label: string;
  category: LeaderboardCategory;
}

@Component({
    selector: 'app-tavern',
    imports: [
        DatePipe,
        DefaultHeaderComponent,
        DropdownComponent,
        NgClass,
        NgFor,
        NgIf,
        NumberFormatPipe,
    ],
    templateUrl: './tavern.component.html'
})
export class TavernComponent implements OnInit {
  readonly podiumOrder = [0, 1, 2];
  readonly categories: readonly LeaderboardCategory[] = [
    'Overall',
    'PvE',
    'PvP',
    'Professions',
    'Guilds',
  ];
  readonly boards: BoardOption[] = [
    { key: 'total-level', label: 'Total Level', category: 'Overall' },
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
    { key: 'arena-rating', label: 'Arena Rating', category: 'PvP' },
    { key: 'tournament-points', label: 'Tournament Points', category: 'PvP' },
    { key: 'profession-crafting', label: 'Crafting', category: 'Professions' },
    { key: 'profession-mining', label: 'Mining', category: 'Professions' },
    {
      key: 'profession-woodcutting',
      label: 'Woodcutting',
      category: 'Professions',
    },
    { key: 'profession-fishing', label: 'Fishing', category: 'Professions' },
    { key: 'profession-skinning', label: 'Skinning', category: 'Professions' },
    {
      key: 'weekly-guild-contribution',
      label: 'Weekly Contribution',
      category: 'Guilds',
    },
    { key: 'guild-renown', label: 'Guild Renown', category: 'Guilds' },
  ];

  activeCategory: LeaderboardCategory = 'Overall';
  activeBoardKey = 'total-level';

  constructor(readonly state: LeaderboardStateService) {}

  ngOnInit(): void {
    this.state.load(this.activeBoardKey);
  }

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
    if (boardKey === this.activeBoardKey) return;
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

  tableEntries(board: LeaderboardBoard): LeaderboardBoardEntry[] {
    return board.entries.slice(this.showPodium(board) ? 3 : 0);
  }

  trackByParticipantId = (_: number, entry: { participantId: string }) =>
    entry.participantId;
}
