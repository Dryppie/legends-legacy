import {
  Component,
  ElementRef,
  effect,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { Subscription } from 'rxjs';
import { DatePipe, NgClass, NgFor, NgIf } from '@angular/common';
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
    CharacterTagComponent,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent implements OnInit, OnDestroy {
  @Output() close = new EventEmitter<void>();
  @ViewChild('chatInput') chatInput?: ElementRef<HTMLInputElement>;
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
    {
      label: 'System',
      contextKey: 'system',
      channelType: ChatChannelType.System,
    },
  ];

  get visibleRooms(): ChatRoom[] {
    return this.availableRooms.filter(
      (r) => !r.requiresGuild || !!this.guild(),
    );
  }

  messages: ChatMessageDto[] = [];
  draft = '';
  sendError = '';
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
    this.sub.add(
      this.chat.whisperDraftTarget$.subscribe((targetName) => {
        if (!targetName) return;

        this.activeChannel = {
          type: ChatChannelType.Whisper,
          contextKey: 'whisper',
        };
        this.draft = `/w ${targetName} `;
        this.focusChatInput();
      }),
    );
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

  channelLabel(message: ChatMessageDto): string {
    if (message.channelType === ChatChannelType.General) {
      return message.contextKey === 'trade' || message.contextKey === 'help'
        ? message.contextKey
        : 'general';
    }

    return message.channelType;
  }

  channelBadgeClasses(message: ChatMessageDto): string {
    switch (message.channelType) {
      case ChatChannelType.Trade:
        return 'border-emerald-400/30 bg-emerald-400/10 text-emerald-300';
      case ChatChannelType.Help:
        return 'border-sky-400/30 bg-sky-400/10 text-sky-300';
      case ChatChannelType.Guild:
        return 'border-rose-400/30 bg-rose-400/10 text-rose-300';
      case ChatChannelType.Whisper:
        return 'border-fuchsia-400/30 bg-fuchsia-400/10 text-fuchsia-300';
      case ChatChannelType.System:
        return 'border-zinc-400/25 bg-zinc-400/10 text-zinc-300';
      default:
        return 'border-primary/30 bg-primary/10 text-primary';
    }
  }

  messageRowClasses(message: ChatMessageDto): string {
    switch (message.channelType) {
      case ChatChannelType.Trade:
        return 'border-l-emerald-400/40';
      case ChatChannelType.Help:
        return 'border-l-sky-400/40';
      case ChatChannelType.Guild:
        return 'border-l-rose-400/40';
      case ChatChannelType.Whisper:
        return 'border-l-fuchsia-400/50 bg-fuchsia-950/10';
      case ChatChannelType.System:
        return 'border-l-zinc-400/40 bg-zinc-900/20';
      default:
        return 'border-l-primary/40';
    }
  }

  whisperDisplayId(message: ChatMessageDto): string {
    return message.senderId === this.characterId()
      ? (message.targetCharacterId ?? '')
      : message.senderId;
  }

  whisperDisplayName(message: ChatMessageDto): string {
    return message.senderId === this.characterId()
      ? (message.targetCharacterName ?? '')
      : message.senderName;
  }

  onDraftChange(): void {
    this.sendError = '';
    if (this.draft.length > 200) {
      this.draft = this.draft.slice(0, 200);
    }
  }

  private focusChatInput(): void {
    setTimeout(() => {
      const input = this.chatInput?.nativeElement;
      if (!input) return;

      input.focus();
      input.setSelectionRange(input.value.length, input.value.length);
    });
  }

  async send(): Promise<void> {
    const body = this.draft.trim();
    if (!body || !isMessageAllowed(body)) return;

    let { type, contextKey } = this.activeChannel;

    try {
      if (body.startsWith('/w ')) {
        const parts = body.split(' ');
        if (parts.length < 3) return; // Invalid

        const targetName = parts[1];
        const messageBody = body
          .slice(body.indexOf(targetName) + targetName.length)
          .trim();

        await this.chat.sendWhisperToName(targetName, messageBody);
        this.draft = '';
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
        default:
          return;
      }

      this.draft = '';
      this.sendError = '';
    } catch (err) {
      this.sendError =
        err instanceof Error ? err.message : 'Unable to send chat message.';
      console.warn('Unable to send chat message.', err);
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
