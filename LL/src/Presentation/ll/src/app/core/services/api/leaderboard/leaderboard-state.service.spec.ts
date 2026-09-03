import { TestBed } from '@angular/core/testing';
import { Subject } from 'rxjs';
import { LeaderboardBoard } from '../../../../shared/models/Dtos/leaderboard/leaderboard';
import { LeaderboardService } from './leaderboard.service';
import { LeaderboardStateService } from './leaderboard-state.service';

describe('LeaderboardStateService', () => {
  let state: LeaderboardStateService;
  let service: jasmine.SpyObj<LeaderboardService>;
  let firstRequest: Subject<LeaderboardBoard>;
  let secondRequest: Subject<LeaderboardBoard>;

  beforeEach(() => {
    firstRequest = new Subject<LeaderboardBoard>();
    secondRequest = new Subject<LeaderboardBoard>();
    service = jasmine.createSpyObj<LeaderboardService>('LeaderboardService', [
      'getLeaderboard',
    ]);
    service.getLeaderboard.and.callFake((key: string) =>
      key === 'total-level' ? firstRequest : secondRequest,
    );

    TestBed.configureTestingModule({
      providers: [
        LeaderboardStateService,
        { provide: LeaderboardService, useValue: service },
      ],
    });
    state = TestBed.inject(LeaderboardStateService);
  });

  it('ignores a stale response after the selected board changes', () => {
    state.load('total-level');
    state.load('combat-level');

    firstRequest.next(board('total-level'));
    secondRequest.next(board('combat-level'));

    expect(state.board()?.key).toBe('combat-level');
    expect(state.activeKey()).toBe('combat-level');
  });

  it('clears cached standings and ignores previous-character responses after reset', () => {
    state.load('total-level');
    firstRequest.next(board('total-level'));
    state.reset();
    expect(state.board()).toBeNull();
    expect(state.activeKey()).toBeNull();
    expect(state.error()).toBeNull();
    expect(state.loading()).toBeFalse();

    state.load('combat-level');
    firstRequest.next(board('total-level'));
    firstRequest.complete();
    expect(state.board()).toBeNull();
    expect(state.loading()).toBeTrue();
    secondRequest.next(board('combat-level'));
    secondRequest.complete();
    expect(state.board()?.key).toBe('combat-level');
    expect(state.loading()).toBeFalse();
  });

  it('retains the existing board while refreshing', () => {
    state.load('total-level');
    firstRequest.next(board('total-level'));
    firstRequest.complete();

    const refreshRequest = new Subject<LeaderboardBoard>();
    service.getLeaderboard.and.returnValue(refreshRequest);
    state.refresh();

    expect(state.refreshing()).toBeTrue();
    expect(state.board()?.key).toBe('total-level');
  });

  it('forwards page cursors and participant searches', () => {
    state.load('total-level');
    firstRequest.next(board('total-level'));

    const pageRequest = new Subject<LeaderboardBoard>();
    service.getLeaderboard.and.returnValue(pageRequest);
    state.loadPage('next-cursor');

    expect(service.getLeaderboard).toHaveBeenCalledWith(
      'total-level',
      'next-cursor',
      null,
    );

    const searchRequest = new Subject<LeaderboardBoard>();
    service.getLeaderboard.and.returnValue(searchRequest);
    state.jumpToParticipant('  Hero  ');

    expect(service.getLeaderboard).toHaveBeenCalledWith(
      'total-level',
      null,
      'Hero',
    );
  });
});

function board(key: string): LeaderboardBoard {
  return {
    key,
    category: 'Overall',
    title: 'Test',
    description: 'Test board',
    participantLabel: 'Character',
    metricLabel: 'Level',
    secondaryMetricLabel: null,
    periodLabel: 'All-time',
    updatedAt: new Date().toISOString(),
    totalParticipants: 0,
    pageStartRank: 0,
    pageEndRank: 0,
    previousCursor: null,
    nextCursor: null,
    searchQuery: null,
    searchMatch: null,
    isViewerRanked: true,
    viewerUnrankedReason: null,
    entries: [],
    viewerEntry: null,
  };
}
