import {
  Component,
  effect,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';
import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { Subscription } from 'rxjs';
import {
  DatePipe,
  NgClass,
  NgFor,
  NgIf,
  SlicePipe,
  TitleCasePipe,
} from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { StickyScrollDirective } from '../../../shared/directives/sticky-scroll/sticky-scroll.directive';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';

interface ChatRoom {
  label: string;
  contextKey: string;
  channelType: ChatChannelType;
  requiresGuild?: boolean;
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    FormsModule,
    RegularButtonComponent,
    StickyScrollDirective,
    DatePipe,
    SlicePipe,
    CharacterTagComponent,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent implements OnInit, OnDestroy {
  @Output() close = new EventEmitter<void>();
  ChatChannelType = ChatChannelType;
  public activeChannel: {
    type: ChatChannelType;
    contextKey: string;
  } = { type: ChatChannelType.General, contextKey: 'all' };

  readonly guild;
  readonly characterId;

  readonly availableRooms: ChatRoom[] = [
    { label: 'All', contextKey: 'all', channelType: ChatChannelType.General },
    {
      label: 'Whisper',
      contextKey: 'whisper',
      channelType: ChatChannelType.Whisper,
    },
    {
      label: 'General',
      contextKey: 'general',
      channelType: ChatChannelType.General,
    },
    {
      label: 'Guild',
      contextKey: 'guild',
      channelType: ChatChannelType.Guild,
      requiresGuild: true,
    },
    {
      label: 'Trade',
      contextKey: 'trade',
      channelType: ChatChannelType.Trade,
    },
    { label: 'Help', contextKey: 'help', channelType: ChatChannelType.Help },
    // {
    //   label: 'System',
    //   contextKey: 'system',
    //   channelType: ChatChannelType.Public,
    // },
  ];

  get visibleRooms(): ChatRoom[] {
    return this.availableRooms.filter(
      (r) => !r.requiresGuild || !!this.guild(),
    );
  }

  messages: ChatMessageDto[] = [];
  draft = '';
  private sub?: Subscription;

  get activeRoomKey(): string {
    return this.activeChannel.contextKey;
  }

  get activeRoomType(): ChatChannelType {
    return this.activeChannel.type;
  }

  constructor(
    public chat: ChatService,
    private readonly guildState: GuildStateService,
    private readonly characterState: CharacterStateService,
  ) {
    this.guild = this.guildState.guild;
    this.characterId = this.characterState.currentCharacterId;
    // effect(() => {
    //   const id = this.guild()?.id;
    //   if (id) {
    //     this.chat.joinGuildChannel(id);
    //   }
    // });
  }

  ngOnInit(): void {
    this.sub = this.chat.messages$.subscribe((m) => {
      this.messages = m;
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  setChannel(type: ChatChannelType, contextKey: string): void {
    this.activeChannel = { type, contextKey };
  }

  get filteredMessages(): ChatMessageDto[] {
    if (this.activeRoomKey === 'all') {
      return this.messages;
    }
    if (
      this.activeRoomType === ChatChannelType.Guild &&
      this.activeRoomKey === 'guild'
    ) {
      return this.messages.filter(
        (m) =>
          m.channelType === this.activeRoomType &&
          m.contextKey === this.guild()?.id,
      );
    }
    if (this.activeRoomKey === 'whisper') {
      return this.messages.filter((m) => m.channelType === this.activeRoomType);
    }
    return this.messages.filter(
      (m) =>
        m.channelType === this.activeRoomType &&
        m.contextKey === this.activeRoomKey,
    );
  }

  onDraftChange(): void {
    if (this.draft.length > 200) {
      this.draft = this.draft.slice(0, 200);
    }
  }

  async send(): Promise<void> {
    const body = this.draft.trim();
    if (!body || !isMessageAllowed(body)) return;

    this.draft = '';

    let { type, contextKey } = this.activeChannel;

    if (body.startsWith('/w ')) {
      const parts = body.split(' ');
      if (parts.length < 3) return; // Invalid

      const targetName = parts[1];
      const messageBody = body
        .slice(body.indexOf(targetName) + targetName.length)
        .trim();

      try {
        await this.chat.sendWhisperToName(targetName, messageBody);
      } catch (err) {
        // You could show a toast or log error here
        console.warn(err);
      }
      return;
    }

    if (contextKey === 'all') contextKey = 'general';
    switch (type) {
      case ChatChannelType.General:
      case ChatChannelType.Trade:
      case ChatChannelType.Help:
        await this.chat.sendPublic(type, contextKey, body);
        break;
      // case ChatChannelType.Whisper:
      //   if (this.chat.targetUserId) {
      //     await this.chat.sendWhisper(this.chat.targetUserId, body);
      //   }
      //   break;
      case ChatChannelType.Guild:
        await this.chat.sendGuild(this.guild()!.id, body);
        break;
    }
  }
}

const COMBINING_MARKS_PATTERN = /\p{M}/gu;

/**
 * ≥20 consecutive repetitions of the exact same *visible* code‑point.
 *   – Letters, Numbers, Symbols, Punctuation are considered visible.
 */
const REPEATED_CHAR_PATTERN = /([\p{L}\p{N}\p{S}\p{P}])\1{19,}/u;

/** C0 + C1 control codes (except TAB, LF, CR which we strip earlier) */
const CONTROL_CHARS_PATTERN = /[\u0000-\u001F\u007F-\u009F]/u;

/** Explicitly forbidden literals (e.g. Arabic ligature ﷽ U+FDFD) */
const FORBIDDEN_LITERALS = /\uFDFD/u;

// 2️⃣  Public API ------------------------------------------------------------

export interface FilterOptions {
  /** Reject if visible length exceeds this (after trimming whitespace). */
  maxVisibleLength?: number;
  /** Absolute number of combining marks allowed before we reject. */
  maxCombiningChars?: number;
  /** combiningCount / visibleLength > ratio triggers reject. */
  maxCombiningRatio?: number;
}

/**
 * Returns `true` if the message is acceptable, `false` if it should be blocked.
 */
export function isMessageAllowed(
  text: string,
  opts: FilterOptions = {},
): boolean {
  const {
    maxVisibleLength = 4000,
    maxCombiningChars = 20,
    maxCombiningRatio = 0.2,
  } = opts;

  // 1. Strip all whitespace so we can reason about *visible* glyphs.
  const visible = text.replace(/\s+/g, '');

  if (visible.length === 0) return false; // empty
  if (visible.length > maxVisibleLength) return false; // too long

  // 2. Block raw control chars (layout / security hazards)
  if (CONTROL_CHARS_PATTERN.test(text)) return false;

  // 3. Explicit single‑codepoint bans (﷽ etc.)
  if (FORBIDDEN_LITERALS.test(text)) return false;

  // 4. Super‑long runs of the *exact* same glyph
  if (REPEATED_CHAR_PATTERN.test(text)) return false;

  // 5. Zalgo / garbage combining mark density check
  const combiningCount = (text.match(COMBINING_MARKS_PATTERN) || []).length;
  const density = combiningCount / Math.max(1, visible.length);
  if (combiningCount > maxCombiningChars && density > maxCombiningRatio) {
    return false;
  }

  return true; // ✅ looks good
}
