import { signal } from '@angular/core';
import { of, Subject, throwError } from 'rxjs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LeaderboardStateService } from '../../../../core/services/api/leaderboard/leaderboard-state.service';
import { LeaderboardBoard } from '../../../../shared/models/Dtos/leaderboard/leaderboard';
import { TavernComponent } from './tavern.component';
import { Router } from '@angular/router';
import { ChatService } from '../../../../core/services/ll-chat/chat-service/chat.service';

describe('TavernComponent', () => {
  let fixture: ComponentFixture<TavernComponent>;
  const board = signal<LeaderboardBoard | null>(createBoard());
  const state = {
    board,
    loading: signal(false),
    refreshing: signal(false),
    error: signal<string | null>(null),
    load: jasmine.createSpy('load'),
    reset: jasmine.createSpy('reset'),
    loadPage: jasmine.createSpy('loadPage'),
    jumpToParticipant: jasmine.createSpy('jumpToParticipant'),
    clearJump: jasmine.createSpy('clearJump'),
    refresh: jasmine.createSpy('refresh'),
  };
  beforeEach(async () => {
    state.reset.calls.reset();
    state.reset.and.stub();
    state.loading.set(false);
    state.error.set(null);
    state.load.calls.reset();
    state.loadPage.calls.reset();
    state.jumpToParticipant.calls.reset();
    state.clearJump.calls.reset();
    state.refresh.calls.reset();
    board.set(createBoard());

    await TestBed.configureTestingModule({
      imports: [TavernComponent],
      providers: [
        { provide: LeaderboardStateService, useValue: state },
        { provide: Router, useValue: { navigate: jasmine.createSpy() } },
        {
          provide: ChatService,
          useValue: { prepareWhisperToName: jasmine.createSpy() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TavernComponent);
    fixture.detectChanges();
  });

  it('renders the current player rank beside participant search', () => {
    const text = fixture.nativeElement.textContent as string;
    const viewerRank = fixture.nativeElement.querySelector(
      '[data-testid="viewer-rank"]',
    ) as HTMLElement;

    expect(text).toContain('Leaderboard');
    expect(text).toContain('Combat Level');
    expect(viewerRank.textContent).toContain('Your rank:');
    expect(viewerRank.textContent).toContain('#4 of 10');
    expect(text).not.toContain('Your standing');
  });

  it('shows a compact unranked label without a standing card', () => {
    const unrankedBoard = createBoard();
    unrankedBoard.viewerEntry = null;
    unrankedBoard.isViewerRanked = false;
    board.set(unrankedBoard);
    fixture.detectChanges();

    const viewerRank = fixture.nativeElement.querySelector(
      '[data-testid="viewer-rank"]',
    ) as HTMLElement;

    expect(viewerRank.textContent).toContain('Your rank:');
    expect(viewerRank.textContent).toContain('Unranked');
    expect(fixture.nativeElement.textContent).not.toContain('Your standing');
  });

  it('exposes collection, achievement, and dungeon mastery boards', () => {
    expect(
      fixture.componentInstance.categoryBoards.map((option) => option.label),
    ).toContain('Soul Archive');
    expect(
      fixture.componentInstance.categoryBoards.map((option) => option.label),
    ).toContain('Achievement Renown');

    fixture.componentInstance.selectCategory('PvE');
    fixture.detectChanges();

    expect(
      fixture.componentInstance.categoryBoards.map((option) => option.label),
    ).toEqual([
      'Dungeon Mastery',
      'Most Dungeon Clears',
      'Raid Boss Kills',
      "Fastest Hive's Abyss",
      'Fastest Sanguine Horror',
    ]);
    expect(state.load).toHaveBeenCalledWith('dungeon-mastery');
  });

  it('exposes weekly contribution and guild renown boards', () => {
    fixture.componentInstance.selectCategory('Guilds');
    fixture.detectChanges();

    expect(
      fixture.componentInstance.categoryBoards.map((option) => option.label),
    ).toEqual(['Weekly Contribution', 'Guild Renown']);
    expect(state.load).toHaveBeenCalledWith('weekly-guild-contribution');
  });

  it('exposes Arena Rating and Tournament Points in the PvP category', () => {
    fixture.componentInstance.selectCategory('PvP');
    fixture.detectChanges();

    expect(
      fixture.componentInstance.categoryBoards.map((option) => option.label),
    ).toEqual(['Arena Rating', 'Tournament Points']);
    expect(state.load).toHaveBeenCalledWith('arena-rating');
  });

  it('uses category dropdowns to select leaderboard boards', () => {
    fixture.componentInstance.selectLeaderboard({
      main: 'PvP',
      sub: 'Tournament Points',
    });

    expect(fixture.componentInstance.activeCategory).toBe('PvP');
    expect(fixture.componentInstance.activeBoardKey).toBe('tournament-points');
    expect(state.load).toHaveBeenCalledWith('tournament-points');
    expect(fixture.nativeElement.querySelectorAll('app-dropdown').length).toBe(
      4,
    );
  });

  it('loads cursor pages and jumps to a participant by name', () => {
    const pagedBoard = createBoard();
    pagedBoard.nextCursor = 'next-page';
    board.set(pagedBoard);
    fixture.detectChanges();

    const input = fixture.nativeElement.querySelector(
      '#leaderboard-participant-search',
    ) as HTMLInputElement;
    input.value = 'Second';
    const form = input.closest('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));

    const nextButton = [
      ...fixture.nativeElement.querySelectorAll('button'),
    ].find(
      (button: HTMLButtonElement) => button.textContent?.trim() === 'Next',
    ) as HTMLButtonElement;
    nextButton.click();

    expect(state.jumpToParticipant).toHaveBeenCalledWith('Second');
    expect(state.loadPage).toHaveBeenCalledWith('next-page');
  });

  it('renders later pages as table rows without a false podium', () => {
    const pagedBoard = createBoard();
    pagedBoard.pageStartRank = 51;
    pagedBoard.pageEndRank = 54;
    pagedBoard.entries = pagedBoard.entries.map((entry, index) => ({
      ...entry,
      rank: 51 + index,
    }));
    board.set(pagedBoard);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('article').length).toBe(0);
    expect(fixture.nativeElement.textContent).toContain('#51');
  });

  it('renders the top three as distinct, labelled podium positions', () => {
    const podiumEntries = [
      ...fixture.nativeElement.querySelectorAll('[data-testid="podium-entry"]'),
    ] as HTMLElement[];

    expect(podiumEntries.map((entry) => entry.dataset['podiumRank'])).toEqual([
      '1',
      '2',
      '3',
    ]);
    expect(podiumEntries[0].textContent).toContain('First place');
    expect(podiumEntries[1].textContent).toContain('Second place');
    expect(podiumEntries[2].textContent).toContain('Third place');
    expect(podiumEntries[0].getAttribute('aria-label')).toBe('Rank 1: First');
    expect(
      fixture.nativeElement.querySelectorAll('app-character-tag').length,
    ).toBe(4);
  });

  it('keeps page chrome fixed while the standings own vertical scrolling', () => {
    const page = fixture.nativeElement.querySelector(
      '[data-testid="leaderboard-page"]',
    ) as HTMLElement;
    const standings = fixture.nativeElement.querySelector(
      '[data-testid="standings-scroll"]',
    ) as HTMLElement;

    expect(page.classList).toContain('overflow-hidden');
    expect(page.classList).not.toContain('overflow-y-auto');
    expect(standings.classList).toContain('overflow-y-auto');
  });

  it('uses the configured participant label and viewer participant id', () => {
    const guildBoard = createBoard();
    guildBoard.participantLabel = 'Guild';
    guildBoard.viewerEntry = {
      participantId: 'viewer-guild',
      participantName: 'Viewer Guild',
      rank: 4,
      primaryValue: 10,
      secondaryValue: null,
    };
    board.set(guildBoard);
    fixture.detectChanges();

    const participantHeader = fixture.nativeElement.querySelector(
      'thead th:nth-child(2)',
    ) as HTMLTableCellElement;

    expect(participantHeader.textContent?.trim()).toBe('Guild');
    expect(fixture.componentInstance.isViewer('viewer-guild')).toBeTrue();
    expect(
      fixture.nativeElement.querySelectorAll('app-character-tag').length,
    ).toBe(0);
  });
});

function createBoard(): LeaderboardBoard {
  return {
    key: 'combat-level',
    category: 'Overall',
    title: 'Combat Level',
    description: 'Combined progression.',
    participantLabel: 'Character',
    metricLabel: 'Total level',
    secondaryMetricLabel: null,
    periodLabel: 'All-time',
    updatedAt: '2026-07-16T12:00:00Z',
    totalParticipants: 10,
    pageStartRank: 1,
    pageEndRank: 4,
    previousCursor: null,
    nextCursor: null,
    searchQuery: null,
    searchMatch: null,
    isViewerRanked: true,
    viewerUnrankedReason: null,
    entries: [
      {
        participantId: 'one',
        participantName: 'First',
        rank: 1,
        primaryValue: 30,
        secondaryValue: null,
      },
      {
        participantId: 'two',
        participantName: 'Second',
        rank: 2,
        primaryValue: 20,
        secondaryValue: null,
      },
      {
        participantId: 'three',
        participantName: 'Third',
        rank: 3,
        primaryValue: 15,
        secondaryValue: null,
      },
      {
        participantId: 'viewer',
        participantName: 'Viewer',
        rank: 4,
        primaryValue: 10,
        secondaryValue: null,
      },
    ],
    viewerEntry: {
      participantId: 'viewer',
      participantName: 'Viewer',
      rank: 4,
      primaryValue: 10,
      secondaryValue: null,
    },
  };
}
