import { provideFreeNobilityForTests } from '../../../../../../core/services/api/nobility/nobility.testing';
import { signal } from '@angular/core';
import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { Router } from '@angular/router';
import { CharacterService } from '../../../../../../core/services/api/character/character.service';
import { GuildStateService } from '../../../../../../core/services/api/guild/guild-state.service';
import { ChatService } from '../../../../../../core/services/ll-chat/chat-service/chat.service';
import { Guild } from '../../../../../../shared/models/Dtos/guild/guild';
import { GuildRole } from '../../../../../../shared/models/Dtos/guild/guildRole';
import { GuildInfoComponent } from './guild-info.component';

describe('GuildInfoComponent', () => {
  let fixture: ComponentFixture<GuildInfoComponent>;
  let suggestCharacterNames: jasmine.Spy;

  beforeEach(async () => {
    const guild = createGuild();
    suggestCharacterNames = jasmine
      .createSpy()
      .and.returnValue(of(['Ember Knight', 'Ember Mage']));

    await TestBed.configureTestingModule({
      imports: [GuildInfoComponent],
      providers: [...provideFreeNobilityForTests(),
        {
          provide: CharacterService,
          useValue: {
            currentCharacterId: signal('current-character'),
            suggestCharacterNames,
          },
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

  it('debounces name lookup and selects a suggestion before sending the invite', fakeAsync(() => {
    const component = fixture.componentInstance;
    const invited = spyOn(component.inviteEvent, 'emit');
    component.closeApplicationsModal();
    component.openModal();
    fixture.detectChanges();
    tick();
    const input: HTMLInputElement =
      fixture.nativeElement.querySelector('#guild-invite-name');
    input.value = 'E';
    input.dispatchEvent(new Event('input'));
    tick(200);
    expect(suggestCharacterNames).not.toHaveBeenCalled();

    input.value = ' Em ';
    input.dispatchEvent(new Event('input'));
    tick(199);
    expect(suggestCharacterNames).not.toHaveBeenCalled();
    tick(1);
    fixture.detectChanges();
    expect(suggestCharacterNames).toHaveBeenCalledOnceWith('Em');
    expect(
      fixture.nativeElement.querySelectorAll('[role="option"]').length,
    ).toBe(2);

    input.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }),
    );
    input.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }),
    );
    expect(component.inviteName).toBe('Ember Mage');
    expect(component.inviteSuggestionsOpen()).toBeFalse();
    expect(invited).not.toHaveBeenCalled();
    component.invite();
    expect(invited).toHaveBeenCalledOnceWith('Ember Mage');
    expect(component.showModal).toBeFalse();
    expect(component.inviteName).toBe('');
  }));

  it('discards old searches and cancels pending lookup when the dialog closes', fakeAsync(() => {
    const component = fixture.componentInstance;
    const oldSearch = new Subject<string[]>();
    suggestCharacterNames.and.returnValue(oldSearch);
    component.onInviteNameChange('Em');
    tick(200);
    component.onInviteNameChange('Ar');
    oldSearch.next(['Ember Knight']);
    expect(component.inviteSuggestions()).toEqual([]);
    component.closeModal();
    tick(200);
    expect(suggestCharacterNames).toHaveBeenCalledTimes(1);
    expect(component.inviteSearchLoading()).toBeFalse();
    expect(component.inviteSuggestionsOpen()).toBeFalse();
  }));

  it('selects a name by clicking and dismisses suggestions before closing the dialog with Escape', fakeAsync(() => {
    const component = fixture.componentInstance;
    component.closeApplicationsModal();
    component.openModal();
    fixture.detectChanges();
    tick();
    component.onInviteNameChange('Em');
    tick(200);
    fixture.detectChanges();
    fixture.nativeElement.querySelector('[role="option"]').click();
    expect(component.inviteName).toBe('Ember Knight');
    expect(component.showModal).toBeTrue();

    component.openInviteSuggestions();
    const input: HTMLInputElement =
      fixture.nativeElement.querySelector('#guild-invite-name');
    input.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    expect(component.inviteSuggestionsOpen()).toBeFalse();
    expect(component.showModal).toBeTrue();
    input.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }),
    );
    expect(component.showModal).toBeFalse();
    tick(200);
  }));

  it('shows lookup failures and allows retrying the same query', fakeAsync(() => {
    const component = fixture.componentInstance;
    suggestCharacterNames.and.returnValue(
      throwError(() => new Error('Lookup failed')),
    );
    component.onInviteNameChange('Em');
    tick(200);
    expect(component.inviteSearchError()).toBeTrue();
    expect(component.inviteSearchLoading()).toBeFalse();
    component.closeInviteSuggestions();
    suggestCharacterNames.and.returnValue(of([]));
    component.openInviteSuggestions();
    tick(200);
    expect(component.inviteSearchError()).toBeFalse();
    expect(component.inviteSuggestions()).toEqual([]);
    expect(suggestCharacterNames).toHaveBeenCalledTimes(2);
  }));
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
        isOnline: true,
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
    rolePermissions: [],
    vaultItems: [],
  };
}
