import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { ChatService } from '../../../../../../core/services/ll-chat/chat-service/chat.service';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { GuildRole } from '../../../../../../shared/models/Dtos/guild/guildRole';
import { GuildInfoComponent } from './guild-info.component';

describe('GuildInfoComponent', () => {
  let fixture: ComponentFixture<GuildInfoComponent>;

  beforeEach(async () => {
    const guild = createGuild();

    await TestBed.configureTestingModule({
      imports: [GuildInfoComponent],
      providers: [
        {
          provide: CharacterService,
          useValue: { currentCharacterId: signal('current-character') },
        },
        { provide: GuildStateService, useValue: { guild: signal(guild) } },
        { provide: Router, useValue: { navigate: jasmine.createSpy() } },
        {
          provide: ChatService,
          useValue: { prepareWhisperToName: jasmine.createSpy() },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(GuildInfoComponent);
    fixture.componentRef.setInput('guild', guild);
    fixture.componentInstance.showApplicationsModal = true;
    fixture.detectChanges();
  });

  it('renders guild applicants as interactive character tags', () => {
    const applicantTags = fixture.nativeElement.querySelectorAll(
      '[data-testid="guild-application-character"]',
    );

    expect(applicantTags.length).toBe(1);
    expect(applicantTags[0].textContent).toContain('Applicant');
  });
});

function createGuild(): Guild {
  return {
    id: 'guild-id',
    name: 'Test Guild',
    tag: '',
    guildXp: 0,
    guildLevel: 1,
    maxMembers: 11,
    members: [
      {
        characterId: 'current-character',
        name: 'Current Player',
        level: 10,
        role: GuildRole.Leader,
        joinedAt: '2026-08-01T00:00:00Z',
      },
    ],
    invites: [
      {
        characterId: 'applicant-character',
        characterName: 'Applicant',
        guildId: 'guild-id',
        guildName: 'Test Guild',
        isInvite: false,
      },
    ],
    resources: [],
  };
}
