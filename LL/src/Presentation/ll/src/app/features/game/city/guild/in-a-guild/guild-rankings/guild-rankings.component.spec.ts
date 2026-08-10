import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { LeaderboardStateService } from '../../../../../../core/services/api/leaderboard/leaderboard-state.service';
import { LeaderboardBoard } from '../../../../../../shared/models/Dtos/leaderboard/leaderboard';
import { GuildRankingsComponent } from './guild-rankings.component';
import { Router } from '@angular/router';
import { ChatService } from '../../../../../../core/services/ll-chat/chat-service/chat.service';

describe('GuildRankingsComponent', () => {
  let fixture: ComponentFixture<GuildRankingsComponent>;
  let leaderboardState: {
    board: ReturnType<typeof signal<LeaderboardBoard | null>>;
    loading: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: jasmine.Spy;
  };

  beforeEach(async () => {
    const board = signal<LeaderboardBoard | null>(createBoard());
    leaderboardState = {
      board,
      loading: signal(false),
      error: signal<string | null>(null),
      load: jasmine.createSpy('load'),
    };

    await TestBed.configureTestingModule({
      imports: [GuildRankingsComponent],
      providers: [
        {
          provide: GuildStateService,
          useValue: {
            allGuilds: signal([
              {
                id: 'lower-renown',
                name: 'Busy Guild',
                ownerName: 'Busy Owner',
                memberCount: 10,
                maxMembers: 20,
                upgrades: 50,
              },
              {
                id: 'higher-renown',
                name: 'Veteran Guild',
                ownerName: 'Veteran Owner',
                memberCount: 1,
                maxMembers: 11,
                upgrades: 1,
              },
            ]),
          },
        },
        { provide: LeaderboardStateService, useValue: leaderboardState },
        { provide: Router, useValue: { navigate: jasmine.createSpy() } },
        {
          provide: ChatService,
          useValue: { prepareWhisperToName: jasmine.createSpy() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GuildRankingsComponent);
    fixture.detectChanges();
  });

  it('uses Guild Renown ranks instead of sorting the guild directory', () => {
    expect(leaderboardState.load).toHaveBeenCalledWith('guild-renown', true);
    expect(
      fixture.componentInstance
        .rankedGuilds()
        .map((guild) => guild.participantId),
    ).toEqual(['higher-renown', 'lower-renown']);

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('Ranked by guild level, then guild experience.');
    expect(text.indexOf('Veteran Guild')).toBeLessThan(
      text.indexOf('Busy Guild'),
    );
    expect(text).toContain('Guild XP');
    expect(text).not.toContain('Upgrades');
    expect(
      fixture.nativeElement.querySelectorAll('app-character-tag').length,
    ).toBe(2);
  });
});

function createBoard(): LeaderboardBoard {
  return {
    key: 'guild-renown',
    category: 'Guilds',
    title: 'Guild Renown',
    description: "The realm's most established guilds.",
    participantLabel: 'Guild',
    metricLabel: 'Guild level',
    secondaryMetricLabel: 'Guild experience',
    periodLabel: 'All-time',
    updatedAt: '2026-08-01T00:00:00Z',
    totalParticipants: 2,
    pageStartRank: 1,
    pageEndRank: 2,
    previousCursor: null,
    nextCursor: null,
    searchQuery: null,
    searchMatch: null,
    isViewerRanked: true,
    viewerUnrankedReason: null,
    entries: [
      {
        participantId: 'higher-renown',
        participantName: 'Veteran Guild',
        rank: 1,
        primaryValue: 5,
        secondaryValue: 45_000,
      },
      {
        participantId: 'lower-renown',
        participantName: 'Busy Guild',
        rank: 2,
        primaryValue: 4,
        secondaryValue: 39_000,
      },
    ],
    viewerEntry: {
      participantId: 'higher-renown',
      participantName: 'Veteran Guild',
      rank: 1,
      primaryValue: 5,
      secondaryValue: 45_000,
    },
  };
}
