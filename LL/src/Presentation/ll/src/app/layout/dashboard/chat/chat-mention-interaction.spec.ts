import { NO_ERRORS_SCHEMA, signal } from '@angular/core';
import {
  ComponentFixture,
  fakeAsync,
  TestBed,
  tick,
} from '@angular/core/testing';
import { OverlayContainer } from '@angular/cdk/overlay';
import { provideRouter, Router } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { ChatComponent } from './chat.component';
import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { RaidService } from '../../../core/services/api/raid/raid.service';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { ChatEquipmentLinkService } from '../../../core/services/client-side/chat-equipment-link/chat-equipment-link.service';
import { EquipmentInstance } from '../../../shared/models/item';

describe('Chat mention interaction', () => {
  let fixture: ComponentFixture<ChatComponent>;
  let overlay: HTMLElement;
  let sendPublic: jasmine.Spy;
  let sendWhisper: jasmine.Spy;
  let resolveName: jasmine.Spy;

  beforeEach(() => {
    const userInfo = { isRegisteredUser: true };
    sendPublic = jasmine.createSpy('sendPublic').and.resolveTo();
    sendWhisper = jasmine.createSpy('sendWhisperToName').and.resolveTo();
    const whisperDraft = new Subject<string>();
    const playerIds: Record<string, string> = {
      ash: 'me',
      ember: 'ember-id',
      'ember knight': 'knight-id',
    };
    resolveName = jasmine
      .createSpy('resolveCharacterIdByName')
      .and.callFake((name: string) =>
        playerIds[name.toLowerCase()]
          ? of(playerIds[name.toLowerCase()])
          : throwError(() => new Error('not found')),
      );
    TestBed.configureTestingModule({
      imports: [ChatComponent],
      providers: [
        provideRouter([]),
        {
          provide: EquipmentService,
          useValue: {
            getLinkedEquipment: () => throwError(() => ({ status: 404 })),
          },
        },
        {
          provide: ChatService,
          useValue: {
            messages$: of([]),
            whisperDraftTarget$: whisperDraft,
            prepareWhisperToName: (name: string) => whisperDraft.next(name),
            sendWhisperToName: sendWhisper,
            onlinePlayerCount: signal(2),
            sendPublic,
          },
        },
        {
          provide: CharacterService,
          useValue: {
            suggestCharacterNames: () => of(['Ember', 'Ember Knight']),
            resolveCharacterIdByName: resolveName,
          },
        },
        {
          provide: CharacterStateService,
          useValue: {
            currentCharacter: signal({ id: 'me', name: 'Ash' }),
            currentCharacterId: signal('me'),
          },
        },
        { provide: GuildStateService, useValue: { guild: signal(null) } },
        { provide: RaidService, useValue: { activeRaidChatId: signal(null) } },
        {
          provide: AuthService,
          useValue: {
            userInfo: signal(userInfo),
            getUserInfo: () => of(userInfo),
          },
        },
      ],
    }).overrideComponent(ChatComponent, {
      remove: { imports: [CharacterTagComponent, ItemComponent] },
      add: { schemas: [NO_ERRORS_SCHEMA] },
    });
    fixture = TestBed.createComponent(ChatComponent);
    overlay = TestBed.inject(OverlayContainer).getContainerElement();
  });

  afterEach(() => fixture.destroy());

  it('opens collapsed chat and sends the selected equipment with existing draft text', fakeAsync(() => {
    fixture.detectChanges();
    fixture.componentInstance.collapsed = true;
    fixture.componentInstance.draft = '/trade Selling this';
    const expand = spyOn(fixture.componentInstance.collapsedChange, 'emit');
    TestBed.inject(ChatEquipmentLinkService).prepare({
      id: '2b84eb39-110d-4b01-aacd-72caef024eba',
      displayName: 'Phoenix Mace',
    } as EquipmentInstance);
    expect(expand).toHaveBeenCalledWith(false);
    expect(fixture.componentInstance.draft).toBe(
      '/trade Selling this [Phoenix Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)',
    );
    expect(sendPublic).not.toHaveBeenCalled();
    fixture.componentInstance.collapsed = false;
    fixture.detectChanges();
    expect(
      fixture.nativeElement.querySelector(
        '[appChatComposer] [contenteditable="false"]',
      ),
    ).not.toBeNull();
    fixture.componentInstance.send();
    tick();
    expect(sendPublic).toHaveBeenCalledWith(
      ChatChannelType.Trade,
      'trade',
      'Selling this [Phoenix Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)',
    );

    expect(fixture.componentInstance.draft).toBe('');
  }));

  it('deletes an inline piece as a unit and keeps an oversized draft intact', fakeAsync(() => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    component.insertEquipmentLink(
      '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)',
    );
    fixture.detectChanges();
    const editor = component.chatInput!;
    editor.setSelectionRange(component.draft.length, component.draft.length);
    key(editor.nativeElement, 'Backspace');
    expect(component.draft).toBe('');
    component.draft = 'x'.repeat(196);
    component.insertEquipmentLink(
      '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)',
    );
    expect(component.draft).toBe('x'.repeat(196));
    expect(component.sendError).toContain('not enough room');
    tick();
  }));

  it('sends a piece without prose and retains it when delivery fails', fakeAsync(() => {
    fixture.detectChanges();
    const component = fixture.componentInstance;
    const token = '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)';
    component.insertEquipmentLink(token);
    sendPublic.and.rejectWith(new Error('Offline'));
    component.send();
    tick();
    expect(component.draft).toBe(token);
    sendPublic.and.resolveTo();
    component.send();
    tick();
    expect(sendPublic).toHaveBeenCalledWith(
      ChatChannelType.General,
      'general',
      token,
    );
    expect(component.draft).toBe('');
  }));

  function typeDraft(body: string, caret = body.length): HTMLElement {
    fixture.detectChanges();
    tick();
    const editor = fixture.componentInstance.chatInput!;
    editor.replaceValue(body, caret);
    fixture.detectChanges();
    return editor.nativeElement;
  }

  it('counts names when inserting multiple pieces, completing mentions, and sending a payload over 200 characters', fakeAsync(() => {
    typeDraft('');
    const component = fixture.componentInstance;
    const token =
      '[Arcane Crossbow](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)';
    for (let i = 0; i < 4; i++) component.insertEquipmentLink(token);
    tick();
    fixture.detectChanges();
    expect(component.draft).toBe(Array(4).fill(token).join(' '));
    expect(component.draft.length).toBeGreaterThan(200);
    expect(component.draftMessageLength).toBe(63);
    const input = typeDraft(component.draft + ' @Em');
    tick(200);
    fixture.detectChanges();
    key(input, 'Enter');
    expect(component.draftMessageLength).toBe(71);
    expect(
      component.chatInput!.nativeElement.querySelectorAll('[data-chat-token]')
        .length,
    ).toBe(4);
    key(input, 'Enter');
    tick();
    expect(sendPublic).toHaveBeenCalledOnceWith(
      ChatChannelType.General,
      'general',
      Array(4).fill(token).join(' ') + ' @Ember',
    );
  }));

  it('inserts several pieces at the saved caret and sends them in their text order', fakeAsync(() => {
    typeDraft('/trade Selling today', 15);
    const component = fixture.componentInstance;
    const first = '[Mace](equipment:2b84eb39-110d-4b01-aacd-72caef024eba)';
    const second = '[Axe](equipment:2b84eb39-110d-4b01-aacd-72caef024ebb)';
    component.insertEquipmentLink(first);
    component.insertEquipmentLink(second);
    tick();
    fixture.detectChanges();
    expect(component.chatInput!.nativeElement.textContent).toBe(
      '/trade Selling [Mace] [Axe] today',
    );
    expect(
      fixture.nativeElement.querySelectorAll(
        '[appChatComposer] [data-chat-token]',
      ).length,
    ).toBe(2);
    expect(
      fixture.nativeElement.querySelector('[aria-label="Remove linked piece"]'),
    ).toBeNull();
    component.send();
    tick();
    expect(sendPublic).toHaveBeenCalledWith(
      ChatChannelType.Trade,
      'trade',
      `Selling ${first} ${second} today`,
    );
    expect(component.draft).toBe('');
  }));

  function key(input: HTMLElement, value: string): void {
    input.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: value,
        bubbles: true,
        cancelable: true,
      }),
    );
    fixture.detectChanges();
  }

  it('browses suggestions and uses Enter to insert, then Enter to send', fakeAsync(() => {
    const input = typeDraft('Hi @Em');
    key(input, 'Enter');
    expect(sendPublic).not.toHaveBeenCalled();
    tick(200);
    fixture.detectChanges();
    expect(overlay.querySelectorAll('[role="option"]').length).toBe(2);
    expect(input.getAttribute('aria-expanded')).toBe('true');
    key(input, 'ArrowDown');
    tick();
    key(input, 'Enter');
    expect(fixture.componentInstance.draft).toBe('Hi @"Ember Knight" ');
    expect(fixture.componentInstance.chatInput!.selectionStart).toBe(
      fixture.componentInstance.chatInput!.value.length,
    );
    expect(sendPublic).not.toHaveBeenCalled();
    expect(overlay.querySelector('[role="listbox"]')).toBeNull();
    key(input, 'Enter');
    tick();
    expect(sendPublic).toHaveBeenCalledOnceWith(
      ChatChannelType.General,
      'general',
      'Hi @"Ember Knight"',
    );
  }));

  it('selects a suggestion by click in mobile chat and preserves following text', fakeAsync(() => {
    fixture.componentRef.setInput('mobileDock', true);
    const input = typeDraft('Hi @Em, ready?', 6);
    tick(200);
    fixture.detectChanges();
    const option = overlay.querySelector(
      '[role="option"]',
    ) as HTMLButtonElement;
    option.dispatchEvent(
      new PointerEvent('pointerdown', {
        bubbles: true,
        cancelable: true,
        pointerType: 'touch',
      }),
    );
    option.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.draft).toBe('Hi @Ember, ready?');
    expect(fixture.componentInstance.chatInput!.selectionStart).toBe(9);
    expect(sendPublic).not.toHaveBeenCalled();
  }));

  it('supports Tab selection and Escape dismissal without sending', fakeAsync(() => {
    const input = typeDraft('@Em');
    tick(200);
    fixture.detectChanges();
    key(input, 'Tab');
    expect(fixture.componentInstance.draft).toBe('@Ember ');
    typeDraft('@Em');
    key(input, 'Escape');
    tick(200);
    fixture.detectChanges();
    expect(overlay.querySelector('[role="listbox"]')).toBeNull();
    expect(sendPublic).not.toHaveBeenCalled();
  }));

  it('does not submit while an IME composition is confirming text', fakeAsync(() => {
    const input = typeDraft('Hello');
    input.dispatchEvent(
      new KeyboardEvent('keydown', {
        key: 'Enter',
        isComposing: true,
        bubbles: true,
      }),
    );
    tick();
    expect(sendPublic).not.toHaveBeenCalled();
  }));

  for (const mobile of [false, true]) {
    it(`highlights only incoming messages mentioning me in ${mobile ? 'mobile' : 'desktop'} chat`, () => {
      fixture.componentRef.setInput('mobileDock', mobile);
      fixture.componentRef.setInput('mobileDockExpanded', mobile);
      fixture.detectChanges();
      const message = (
        id: string,
        senderId: string,
        body: string,
      ): ChatMessageDto => ({
        id,
        senderId,
        senderName: senderId,
        body,
        channelType: ChatChannelType.General,
        contextKey: 'general',
        sentAt: new Date(),
      });
      fixture.componentInstance.messages = [
        message('1', 'other', 'Hey @Ash and @Ember!'),
        message('2', 'me', 'Hey @Ember'),
        message('3', 'me', '@Ash'),
        message('4', 'other', '@Ashen'),
        message('5', 'other', '<img src=x onerror=alert(1)> @"Ember Knight"'),
      ];
      fixture.detectChanges();
      fixture.detectChanges();
      const rows = fixture.nativeElement.querySelectorAll(
        'article',
      ) as NodeListOf<HTMLElement>;
      expect(rows.length).toBe(5);
      expect(rows[0].classList.contains('chat-mentioned-message')).toBeTrue();
      for (const row of Array.from(rows).slice(1))
        expect(row.classList.contains('chat-mentioned-message')).toBeFalse();
      expect(rows[0].querySelectorAll('.chat-mention').length).toBe(2);
      expect(rows[1].querySelector('.chat-mention')?.textContent).toBe(
        '@Ember',
      );
      expect(rows[4].querySelector('.chat-mention')?.textContent).toBe(
        '@Ember Knight',
      );
      expect(rows[4].querySelector('img')).toBeNull();
      expect(rows[4].textContent).toContain('<img src=x onerror=alert(1)>');
    });
  }

  for (const mobile of [false, true]) {
    it(`opens a tagged player's profile and prepares a whisper in ${mobile ? 'mobile' : 'desktop'} chat`, fakeAsync(() => {
      fixture.componentRef.setInput('mobileDock', mobile);
      fixture.componentRef.setInput('mobileDockExpanded', mobile);
      fixture.detectChanges();
      fixture.componentInstance.messages = [
        {
          id: 'menu',
          senderId: 'other',
          senderName: 'Other',
          body: '@"Ember Knight"',
          channelType: ChatChannelType.General,
          contextKey: 'general',
          sentAt: new Date(),
          targetUrl: '/should-not-navigate',
        },
      ];
      const router = TestBed.inject(Router);
      const navigate = spyOn(router, 'navigate').and.resolveTo(true);
      const navigateByUrl = spyOn(router, 'navigateByUrl').and.resolveTo(true);
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
      const trigger = fixture.nativeElement.querySelector(
        'app-chat-mention [role="button"]',
      ) as HTMLElement;
      trigger.click();
      fixture.detectChanges();
      const profile = Array.from(overlay.querySelectorAll('button')).find(
        (button) => button.textContent?.trim() === 'Open Profile',
      )!;
      profile.click();
      expect(navigate).toHaveBeenCalledOnceWith(
        ['/game/character/character-overview'],
        { queryParams: { characterName: 'Ember Knight' } },
      );
      expect(overlay.querySelector('[role="dialog"]')).toBeNull();

      trigger.dispatchEvent(
        new KeyboardEvent('keydown', {
          key: 'Enter',
          bubbles: true,
          cancelable: true,
        }),
      );
      fixture.detectChanges();
      const whisper = Array.from(overlay.querySelectorAll('button')).find(
        (button) => button.textContent?.trim() === 'Whisper',
      )!;
      whisper.click();
      tick();
      fixture.detectChanges();
      expect(navigateByUrl).not.toHaveBeenCalled();
      expect(overlay.querySelector('[role="dialog"]')).toBeNull();
      expect(fixture.componentInstance.draft).toBe('/w "Ember Knight" ');
      expect(sendWhisper).not.toHaveBeenCalled();
      const input = typeDraft('/w "Ember Knight" Hello there');
      key(input, 'Enter');
      tick();
      expect(sendWhisper).toHaveBeenCalledOnceWith(
        'Ember Knight',
        'Hello there',
      );
    }));
  }

  it('keeps unverified and nonexistent mentions as exact plain text, including quoted names', fakeAsync(() => {
    const response = new Subject<string>();
    resolveName.and.returnValue(response);
    fixture.detectChanges();
    fixture.componentInstance.messages = [
      {
        id: 'unknown',
        senderId: 'other',
        senderName: 'Other',
        body: '@blablabla @"Nobody Here"',
        channelType: ChatChannelType.General,
        contextKey: 'general',
        sentAt: new Date(),
      },
    ];
    fixture.detectChanges();
    tick();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chat-mention')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('article').textContent,
    ).toContain('@blablabla @"Nobody Here"');
    response.error(new Error('not found'));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.chat-mention')).toBeNull();
    expect(
      fixture.nativeElement.querySelector('app-chat-mention [role="button"]'),
    ).toBeNull();
    expect(
      fixture.nativeElement.querySelector('.chat-mentioned-message'),
    ).toBeNull();
  }));
});
