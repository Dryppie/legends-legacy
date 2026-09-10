import { signal } from '@angular/core';
import { GuildStateService } from '../../../../../core/services/api/guild/guild-state.service';
import { LeaderboardStateService } from '../../../../../core/services/api/leaderboard/leaderboard-state.service';
import { ChatService } from '../../../../../core/services/ll-chat/chat-service/chat.service';
import { TestBed } from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  provideRouter,
} from '@angular/router';
import { BehaviorSubject, of, Subject, throwError } from 'rxjs';
import { GuildService } from '../../../../../core/services/api/guild/guild.service';
import { GuildPublic } from '../../../../../shared/models/Dtos/guild/guildPublic';
import { GuildRole } from '../../../../../shared/models/Dtos/guild/guildRole';
import { PublicGuildComponent } from './public-guild.component';

describe('PublicGuildComponent', () => {
  const guild: GuildPublic = {
    id: 'other-guild',
    name: 'Other Guild',
    tag: 'OG',
    description: 'A guild for adventurers.',
    maxMembers: 13,
    members: [
      {
        characterId: 'member',
        name: 'Other Leader',
        level: 42,
        role: GuildRole.Leader,
      },
    ],
    buildings: [{ type: 'GuildHall', level: 3 }],
  };
  let params: BehaviorSubject<ReturnType<typeof convertToParamMap>>;
  let getPublicGuild: jasmine.Spy;
  let loadDirectory: jasmine.Spy;
  let loadRankings: jasmine.Spy;

  beforeEach(async () => {
    params = new BehaviorSubject(convertToParamMap({ guildId: guild.id }));
    getPublicGuild = jasmine.createSpy().and.returnValue(of(guild));
    loadDirectory = jasmine.createSpy('loadAllGuilds');
    loadRankings = jasmine.createSpy('load');
    await TestBed.configureTestingModule({
      imports: [PublicGuildComponent],
      providers: [
        provideRouter([]),
        {
          provide: GuildStateService,
          useValue: { allGuilds: signal([]), loadAllGuilds: loadDirectory },
        },
        {
          provide: LeaderboardStateService,
          useValue: {
            board: signal(null),
            loading: signal(false),
            error: signal(null),
            load: loadRankings,
          },
        },
        {
          provide: ChatService,
          useValue: { prepareWhisperToName: jasmine.createSpy() },
        },
        { provide: ActivatedRoute, useValue: { paramMap: params } },
        { provide: GuildService, useValue: { getPublicGuild } },
      ],
    }).compileComponents();
  });

  it('matches the guild page with only Guild, Buildings and Rankings tabs', () => {
    const fixture = TestBed.createComponent(PublicGuildComponent);
    fixture.detectChanges();
    const page: HTMLElement = fixture.nativeElement;
    expect(page.textContent).toContain('Other Guild');
    expect(page.textContent).toContain('Other Leader');
    expect(page.textContent).toContain('42');
    expect(page.textContent).toContain('Guild Headquarters');
    expect(page.textContent).toContain('[OG]');
    expect(page.textContent).toContain('A guild for adventurers.');
    expect(page.textContent).toContain('1 / 13');
    const tabs = Array.from(
      page.querySelectorAll<HTMLButtonElement>('[role="tab"]'),
    );
    expect(tabs.map((tab) => tab.textContent?.trim())).toEqual([
      'Guild',
      'Buildings',
      'Rankings',
    ]);
    expect(page.textContent).not.toContain('Guild Hall');
    for (const hidden of [
      'Favor',
      'Applications',
      'Guild Supplies',
      'Guild Actions',
      'Role Permissions',
      'Invite',
      'Promote',
      'Kick',
      'Edit',
    ]) {
      expect(page.textContent).not.toContain(hidden);
    }
    tabs[1].click();
    fixture.detectChanges();
    expect(page.textContent).toContain('Guild Hall');
    expect(page.textContent).toContain('Level 3');
    expect(page.textContent).not.toContain('Other Leader');
    expect(page.textContent).not.toContain('Guild Supplies');
    expect(page.querySelectorAll('button:not([role="tab"])').length).toBe(0);
    tabs[2].click();
    fixture.detectChanges();
    expect(page.querySelector('app-guild-rankings')).not.toBeNull();
    expect(loadDirectory).toHaveBeenCalled();
    expect(loadRankings).toHaveBeenCalledWith('guild-renown', true);
    expect(page.querySelector('app-guild-link a')?.getAttribute('href')).toBe(
      '/game/city/guild/other-guild',
    );
    expect(getPublicGuild).toHaveBeenCalledOnceWith(guild.id);
  });

  it('clears the previous guild while navigating and ignores a stale response', () => {
    const oldRequest = new Subject<GuildPublic>();
    const newRequest = new Subject<GuildPublic>();
    getPublicGuild.and.returnValues(oldRequest, newRequest);
    const fixture = TestBed.createComponent(PublicGuildComponent);
    fixture.detectChanges();
    params.next(convertToParamMap({ guildId: 'next-guild' }));
    oldRequest.next(guild);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Loading guild');
    expect(fixture.nativeElement.textContent).not.toContain('Other Leader');
    newRequest.next({ ...guild, id: 'next-guild', name: 'Next Guild' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Next Guild');
  });

  it('handles a missing guild and allows a failed request to be retried', () => {
    getPublicGuild.and.returnValue(throwError(() => ({ status: 404 })));
    const fixture = TestBed.createComponent(PublicGuildComponent);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'This guild no longer exists',
    );
    getPublicGuild.and.returnValue(
      of({ ...guild, members: [], buildings: [] }),
    );
    fixture.nativeElement.querySelector('button').click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No members.');
    fixture.nativeElement.querySelectorAll('[role="tab"]')[1].click();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain(
      'No guild buildings constructed yet.',
    );
  });
});
