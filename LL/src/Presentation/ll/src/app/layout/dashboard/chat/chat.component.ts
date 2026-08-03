import {
  Component,
  ElementRef,
  effect,
  EventEmitter,
  Input,
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
import { RouterLink } from '@angular/router';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { StickyScrollDirective } from '../../../shared/directives/sticky-scroll/sticky-scroll.directive';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';

interface ChatRoom {
  label: string;
  contextKey: string;
  channelType: ChatChannelType;
  requiresGuild?: boolean;
}

export function isWorldSystemMessage(message: ChatMessageDto): boolean {
  return (
    message.channelType === ChatChannelType.System &&
    message.senderName.trim().toLowerCase() === 'world'
  );
}

@Component({
  selector: 'app-chat',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    FormsModule,
    RegularButtonComponent,
    StickyScrollDirective,
    DatePipe,
    CharacterTagComponent,
    RouterLink,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent implements OnInit, OnDestroy {
  @Input() collapsible = false;
  @Input() collapsed = false;
  @Input() drawer = false;
  @Input() drawerTall = false;
  @Output() close = new EventEmitter<void>();
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() drawerTallChange = new EventEmitter<boolean>();
  @ViewChild('chatInput') chatInput?: ElementRef<HTMLInputElement>;
  @ViewChild('channelScroller')
  channelScroller?: ElementRef<HTMLElement>;
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
      label: 'Whisper',
      contextKey: 'whisper',
      channelType: ChatChannelType.Whisper,
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
  userInfo: UserInfoDto | null = null;
  userInfoLoaded = false;
  chatAccessFailed = false;
  private sub = new Subscription();
  private channelDragPointerId: number | null = null;
  private channelDragStartX = 0;
  private channelDragStartScrollLeft = 0;
  private channelDragMoved = false;

  get activeRoomKey(): string {
    return this.activeChannel.contextKey;
  }

  get activeRoomType(): ChatChannelType {
    return this.activeChannel.type;
  }

  get isGuestAccount(): boolean {
    return this.userInfoLoaded && this.userInfo?.isRegisteredUser === false;
  }

  get canWriteChat(): boolean {
    return this.userInfoLoaded && this.userInfo?.isRegisteredUser === true;
  }

  get chatPlaceholder(): string {
    if (!this.userInfoLoaded) {
      return 'Checking chat access...';
    }

    if (this.chatAccessFailed) {
      return 'Unable to verify chat access';
    }

    return this.canWriteChat
      ? 'write here..'
      : 'Register your account to write in chat';
  }

  get onlinePlayerLabel(): string {
    const onlinePlayers = this.chat.onlinePlayerCount();
    return onlinePlayers === null ? 'Connecting...' : `${onlinePlayers} online`;
  }

  constructor(
    public chat: ChatService,
    private readonly guildState: GuildStateService,
    private readonly characterState: CharacterStateService,
    private readonly authService: AuthService,
  ) {
    this.guild = this.guildState.guild;
    this.characterId = this.characterState.currentCharacterId;
    effect(() => {
      const userInfo = this.authService.userInfo();
      if (!userInfo) return;

      this.userInfo = userInfo;
      this.userInfoLoaded = true;
      this.chatAccessFailed = false;
      this.sendError = '';
    });
    // effect(() => {
    //   const id = this.guild()?.id;
    //   if (id) {
    //     this.chat.joinGuildChannel(id);
    //   }
    // });
  }

  ngOnInit(): void {
    this.sub.add(
      this.chat.messages$.subscribe((m) => {
        this.messages = m;
      }),
    );
    this.sub.add(
      this.authService.getUserInfo().subscribe({
        next: (userInfo) => {
          this.userInfo = userInfo;
          this.userInfoLoaded = true;
          this.chatAccessFailed = false;
        },
        error: (err) => {
          this.userInfoLoaded = true;
          this.chatAccessFailed = true;
          this.sendError = 'Unable to verify chat access.';
          console.warn('Unable to load user info for chat access.', err);
        },
      }),
    );
    this.sub.add(
      this.chat.whisperDraftTarget$.subscribe((targetName) => {
        if (!targetName) return;

        this.activeChannel = {
          type: ChatChannelType.Whisper,
          contextKey: 'whisper',
        };
        this.draft = `/w ${targetName} `;
        if (this.canWriteChat) {
          this.focusChatInput();
        }
      }),
    );
  }

  ngOnDestroy(): void {
    this.sub.unsubscribe();
  }

  setChannel(type: ChatChannelType, contextKey: string): void {
    this.activeChannel = { type, contextKey };
  }

  onChannelPointerDown(event: PointerEvent): void {
    if (event.pointerType === 'mouse' && event.button !== 0) return;

    const scroller = this.channelScroller?.nativeElement;
    if (!scroller) return;

    this.channelDragPointerId = event.pointerId;
    this.channelDragStartX = event.clientX;
    this.channelDragStartScrollLeft = scroller.scrollLeft;
    this.channelDragMoved = false;
  }

  onChannelPointerMove(event: PointerEvent): void {
    if (this.channelDragPointerId !== event.pointerId) return;

    const scroller = this.channelScroller?.nativeElement;
    if (!scroller) return;

    const distance = event.clientX - this.channelDragStartX;
    if (Math.abs(distance) > 3 && !this.channelDragMoved) {
      this.channelDragMoved = true;
      scroller.setPointerCapture(event.pointerId);
    }

    if (!this.channelDragMoved) return;

    event.preventDefault();
    scroller.scrollLeft = this.channelDragStartScrollLeft - distance;
  }

  onChannelPointerEnd(event: PointerEvent): void {
    if (this.channelDragPointerId !== event.pointerId) return;

    const scroller = this.channelScroller?.nativeElement;
    if (scroller?.hasPointerCapture(event.pointerId)) {
      scroller.releasePointerCapture(event.pointerId);
    }

    this.channelDragPointerId = null;
    setTimeout(() => {
      this.channelDragMoved = false;
    });
  }

  selectChannelFromPointer(
    event: MouseEvent,
    type: ChatChannelType,
    contextKey: string,
  ): void {
    if (this.channelDragMoved) {
      event.preventDefault();
      event.stopPropagation();
      return;
    }

    this.setChannel(type, contextKey);
  }

  toggleCollapsed(): void {
    if (!this.collapsible) return;

    this.collapsedChange.emit(!this.collapsed);
  }

  toggleDrawerHeight(): void {
    if (!this.drawer) return;

    this.drawerTallChange.emit(!this.drawerTall);
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
    if (isWorldSystemMessage(message)) {
      return 'World';
    }

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
        return 'll-badge-warning';
      case ChatChannelType.Help:
        return 'll-badge-info';
      case ChatChannelType.Guild:
        return 'll-badge-accent';
      case ChatChannelType.Whisper:
        return 'll-badge-accent';
      case ChatChannelType.System:
        return isWorldSystemMessage(message)
          ? 'll-badge-warning'
          : 'll-badge-muted';
      default:
        return 'll-badge-accent';
    }
  }

  messageRowClasses(message: ChatMessageDto): string {
    switch (message.channelType) {
      case ChatChannelType.Trade:
        return 'border-l-[var(--ll-color-warning)]';
      case ChatChannelType.Help:
        return 'border-l-[var(--ll-color-info)]';
      case ChatChannelType.Guild:
        return 'border-l-[var(--ll-color-primary)]';
      case ChatChannelType.Whisper:
        return 'border-l-[var(--ll-color-primary-strong)] bg-[var(--ll-color-primary-soft)]';
      case ChatChannelType.System:
        return isWorldSystemMessage(message)
          ? 'border-l-[var(--ll-color-warning)] bg-[var(--ll-color-warning-soft)] shadow-[inset_0_0_18px_rgba(245,158,11,0.05)] hover:bg-[var(--ll-color-warning-soft)]'
          : 'border-l-[var(--ll-color-text-subtle)] bg-[var(--ll-color-surface-soft)]';
      default:
        return 'border-l-[var(--ll-color-border-strong)]';
    }
  }

  isWorldAnnouncement(message: ChatMessageDto): boolean {
    return isWorldSystemMessage(message);
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

  whisperDisplayTitle(message: ChatMessageDto): string | null | undefined {
    return message.senderId === this.characterId()
      ? message.targetCharacterTitleDisplayName
      : message.senderTitleDisplayName;
  }

  onDraftChange(): void {
    this.sendError = '';
    if (!this.canWriteChat) {
      this.draft = '';
      return;
    }

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
    if (!this.canWriteChat) {
      this.sendError = this.isGuestAccount
        ? 'Register your account before writing in chat.'
        : this.chatAccessFailed
          ? 'Unable to verify chat access.'
          : 'Chat access is still loading.';
      return;
    }

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
      this.sendError = getChatSendErrorMessage(err);
      console.warn('Unable to send chat message.', err);
    }
  }
}

export function getChatSendErrorMessage(error: unknown): string {
  const technicalMessage =
    error instanceof Error
      ? error.message
      : typeof error === 'string'
        ? error
        : '';
  const normalizedMessage = technicalMessage.toLowerCase();

  if (
    normalizedMessage.includes('register your account') ||
    normalizedMessage.includes('guest account')
  ) {
    return 'Register your account before writing in chat.';
  }

  if (
    normalizedMessage.includes('not a member') ||
    normalizedMessage.includes('forbidden')
  ) {
    return 'You no longer have access to this chat channel.';
  }

  if (
    normalizedMessage.includes('rate limit') ||
    normalizedMessage.includes('too many')
  ) {
    return "You're sending messages too quickly. Please wait a moment.";
  }

  if (normalizedMessage.includes('not found')) {
    return "That player couldn't be found.";
  }

  const availabilityErrors = [
    'failed to fetch',
    'negotiation',
    'network',
    'connection',
    'disconnected',
    'timeout',
    'unavailable',
  ];
  if (availabilityErrors.some((value) => normalizedMessage.includes(value))) {
    return 'Chat is temporarily unavailable. Check your connection and try again.';
  }

  return "Your message couldn't be sent. Please try again.";
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
