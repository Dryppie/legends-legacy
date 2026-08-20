import {
  Component,
  computed,
  ElementRef,
  effect,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
  Pipe,
  PipeTransform,
  ViewChild,
} from '@angular/core';
import {
  ChatChannelType,
  ChatMessageDto,
  ChatService,
} from '../../../core/services/ll-chat/chat-service/chat.service';
import { firstValueFrom, Subscription } from 'rxjs';
import {
  DatePipe,
  NgClass,
  NgFor,
  NgIf,
  NgTemplateOutlet,
} from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { StickyScrollDirective } from '../../../shared/directives/sticky-scroll/sticky-scroll.directive';
import { GuildStateService } from '../../../core/services/api/guild/guild-state.service';
import { RaidService } from '../../../core/services/api/raid/raid.service';
import { CharacterStateService } from '../../../core/services/api/character/character-state.service';
import { CharacterTagComponent } from '../../../shared/components/character/character-tag/character-tag.component';
import { AuthService } from '../../../core/services/api/auth/auth.service';
import { UserInfoDto } from '../../../shared/models/Dtos/userInfoDto';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { ItemComponent } from '../../../shared/components/item/item.component';
import { environment } from '../../../../environments/environment';

export interface WireCommand {
  recipientName: string;
  amount: number;
}

export type WireCommandParseResult =
  | { isWire: false }
  | { isWire: true; command: WireCommand | null };

export interface ChatTextSegment {
  text: string;
  isCurrentPlayerMention: boolean;
}

const MENTION_DELIMITER_PATTERN = /[\s.,!?;:()[\]{}"']/u;

/**
 * Splits a chat body without creating HTML so Angular can render mentions safely.
 * Only the current player's exact name is marked as a mention on their client.
 */
export function splitCurrentPlayerMentions(
  body: string,
  playerName: string | null | undefined,
): ChatTextSegment[] {
  const trimmedPlayerName = playerName?.trim();
  if (!trimmedPlayerName) {
    return [{ text: body, isCurrentPlayerMention: false }];
  }

  const escapedPlayerName = trimmedPlayerName.replace(
    /[.*+?^${}()|[\]\\]/g,
    '\\$&',
  );
  const mentionPattern = new RegExp(`@${escapedPlayerName}`, 'giu');
  const segments: ChatTextSegment[] = [];
  let bodyCursor = 0;
  let match: RegExpExecArray | null;

  while ((match = mentionPattern.exec(body)) !== null) {
    const mentionStart = match.index;
    const mentionEnd = mentionPattern.lastIndex;
    const previousCharacter = mentionStart > 0 ? body[mentionStart - 1] : null;
    const nextCharacter = mentionEnd < body.length ? body[mentionEnd] : null;
    const hasValidStart =
      previousCharacter === null ||
      MENTION_DELIMITER_PATTERN.test(previousCharacter);
    const hasValidEnd =
      nextCharacter === null || MENTION_DELIMITER_PATTERN.test(nextCharacter);

    if (!hasValidStart || !hasValidEnd) continue;

    if (mentionStart > bodyCursor) {
      segments.push({
        text: body.slice(bodyCursor, mentionStart),
        isCurrentPlayerMention: false,
      });
    }

    segments.push({
      text: body.slice(mentionStart, mentionEnd),
      isCurrentPlayerMention: true,
    });
    bodyCursor = mentionEnd;
  }

  if (bodyCursor < body.length || segments.length === 0) {
    segments.push({
      text: body.slice(bodyCursor),
      isCurrentPlayerMention: false,
    });
  }

  return segments;
}

@Pipe({
  name: 'chatMentionSegments',
  standalone: true,
  pure: true,
})
export class ChatMentionSegmentsPipe implements PipeTransform {
  transform(
    body: string,
    playerName: string | null | undefined,
  ): ChatTextSegment[] {
    return splitCurrentPlayerMentions(body, playerName);
  }
}

export function parseWireCommand(body: string): WireCommandParseResult {
  const trimmed = body.trim();
  if (!/^\/wire(?:\s|$)/i.test(trimmed)) return { isWire: false };

  const match = /^\/wire\s+(.+?)\s+(\d+)\s+cinders\s*$/i.exec(trimmed);
  if (!match) return { isWire: true, command: null };

  const amount = Number(match[2]);
  if (!Number.isSafeInteger(amount) || amount <= 0) {
    return { isWire: true, command: null };
  }

  return {
    isWire: true,
    command: { recipientName: match[1].trim(), amount },
  };
}

interface ChatRoom {
  label: string;
  contextKey: string;
  channelType: ChatChannelType;
  requiresGuild?: boolean;
}

export function fallbackFromUnavailableRaidChannel(
  activeChannel: ActiveChatChannel,
  raidId: string | null,
): ActiveChatChannel {
  return activeChannel.type === ChatChannelType.Raid &&
    activeChannel.contextKey !== raidId
    ? { type: ChatChannelType.General, contextKey: 'all' }
    : activeChannel;
}

export interface ActiveChatChannel {
  type: ChatChannelType;
  contextKey: string;
}

export function fallbackFromUnavailableGuildChannel(
  activeChannel: ActiveChatChannel,
  hasGuild: boolean,
): ActiveChatChannel {
  return !hasGuild && activeChannel.type === ChatChannelType.Guild
    ? { type: ChatChannelType.General, contextKey: 'all' }
    : activeChannel;
}

export function isWorldSystemMessage(message: ChatMessageDto): boolean {
  return (
    message.channelType === ChatChannelType.System &&
    message.senderName.trim().toLowerCase() === 'world'
  );
}

export function isInlineChannelSystemMessage(
  message: Pick<ChatMessageDto, 'channelType' | 'body' | 'isSystemGenerated'>,
): boolean {
  if (
    message.channelType !== ChatChannelType.Guild &&
    message.channelType !== ChatChannelType.Raid
  ) {
    return false;
  }

  return (
    message.isSystemGenerated === true ||
    (message.channelType === ChatChannelType.Guild &&
      message.body
        .trimStart()
        .toLowerCase()
        .startsWith('set the current building target to '))
  );
}

export function startsNewChatDay(
  messages: readonly ChatMessageDto[],
  index: number,
): boolean {
  if (index <= 0) return true;

  return (
    localChatDateKey(messages[index].sentAt) !==
    localChatDateKey(messages[index - 1].sentAt)
  );
}

function localChatDateKey(value: Date | string): string {
  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  return `${date.getFullYear()}-${date.getMonth()}-${date.getDate()}`;
}

@Component({
  selector: 'app-chat',
  imports: [
    NgFor,
    NgIf,
    NgClass,
    NgTemplateOutlet,
    FormsModule,
    RegularButtonComponent,
    StickyScrollDirective,
    DatePipe,
    CharacterTagComponent,
    ItemComponent,
    RouterLink,
    ChatMentionSegmentsPipe,
  ],
  templateUrl: './chat.component.html',
})
export class ChatComponent implements OnInit, OnDestroy {
  @Input() collapsible = false;
  @Input() collapsed = false;
  @Input() drawer = false;
  @Input() drawerTall = false;
  @Input() mobileDock = false;
  @Input() mobileDockExpanded = false;
  @Output() close = new EventEmitter<void>();
  @Output() expand = new EventEmitter<void>();
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() drawerTallChange = new EventEmitter<boolean>();
  @Output() drawerDragStart = new EventEmitter<PointerEvent>();
  @Output() drawerDragMove = new EventEmitter<PointerEvent>();
  @Output() drawerDragEnd = new EventEmitter<PointerEvent>();
  @ViewChild('chatInput') chatInput?: ElementRef<HTMLInputElement>;
  @ViewChild('channelScroller')
  channelScroller?: ElementRef<HTMLElement>;
  ChatChannelType = ChatChannelType;
  public activeChannel: ActiveChatChannel = {
    type: ChatChannelType.General,
    contextKey: 'all',
  };

  readonly guild;
  readonly raidId;
  readonly characterId;
  readonly characterName;

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
    const rooms = this.availableRooms.filter(
      (r) => !r.requiresGuild || !!this.guild(),
    );
    const raidId = this.raidId();
    if (environment.features.raids && raidId) {
      rooms.splice(3, 0, {
        label: 'Raid',
        contextKey: raidId,
        channelType: ChatChannelType.Raid,
      });
    }
    return rooms;
  }

  trackRoom(_: number, room: ChatRoom): string {
    return `${room.channelType}:${room.contextKey}`;
  }

  messages: ChatMessageDto[] = [];
  draft = '';
  sendError = '';
  isSending = false;
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

  get activeMobileRoomLabel(): string {
    return (
      this.visibleRooms.find(
        (room) =>
          room.contextKey === this.activeRoomKey &&
          room.channelType === this.activeRoomType,
      )?.label ?? 'Chat'
    );
  }

  get latestMobileMessage(): ChatMessageDto | undefined {
    return this.filteredMessages[this.filteredMessages.length - 1];
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
    private readonly raidService: RaidService,
    private readonly characterState: CharacterStateService,
    private readonly characterService: CharacterService,
    private readonly authService: AuthService,
    private readonly router: Router,
  ) {
    this.guild = this.guildState.guild;
    this.raidId = this.raidService.activeRaidChatId;
    this.characterId = this.characterState.currentCharacterId;
    this.characterName = computed(
      () => this.characterState.currentCharacter()?.name ?? null,
    );
    effect(() => {
      this.activeChannel = fallbackFromUnavailableGuildChannel(
        this.activeChannel,
        !!this.guild(),
      );
      this.activeChannel = fallbackFromUnavailableRaidChannel(
        this.activeChannel,
        environment.features.raids ? this.raidId() : null,
      );
    });
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

  startDrawerDrag(event: PointerEvent): void {
    if (!this.drawer || (event.pointerType === 'mouse' && event.button !== 0)) {
      return;
    }

    event.preventDefault();
    (event.currentTarget as HTMLElement).setPointerCapture(event.pointerId);
    this.drawerDragStart.emit(event);
  }

  moveDrawer(event: PointerEvent): void {
    const handle = event.currentTarget as HTMLElement;
    if (!this.drawer || !handle.hasPointerCapture(event.pointerId)) return;

    event.preventDefault();
    this.drawerDragMove.emit(event);
  }

  endDrawerDrag(event: PointerEvent): void {
    const handle = event.currentTarget as HTMLElement;
    if (!handle.hasPointerCapture(event.pointerId)) return;

    this.drawerDragEnd.emit(event);
    handle.releasePointerCapture(event.pointerId);
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

  startsNewDay(messages: readonly ChatMessageDto[], index: number): boolean {
    return startsNewChatDay(messages, index);
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
    switch (this.displayChannelType(message)) {
      case ChatChannelType.Trade:
        return 'border-emerald-400/40 bg-emerald-400/10 text-emerald-300';
      case ChatChannelType.Help:
        return 'border-sky-400/40 bg-sky-400/10 text-sky-300';
      case ChatChannelType.Guild:
        return 'border-rose-400/40 bg-rose-400/10 text-rose-300';
      case ChatChannelType.Raid:
        return 'border-orange-400/40 bg-orange-400/10 text-orange-300';
      case ChatChannelType.Whisper:
        return 'border-fuchsia-400/40 bg-fuchsia-400/10 text-fuchsia-300';
      case ChatChannelType.System:
        return isWorldSystemMessage(message)
          ? 'border-amber-400/40 bg-amber-400/10 text-amber-300'
          : 'border-slate-400/40 bg-slate-400/10 text-slate-300';
      default:
        return 'border-primary/40 bg-primary/10 text-primary';
    }
  }

  messageRowClasses(message: ChatMessageDto): string {
    switch (this.displayChannelType(message)) {
      case ChatChannelType.Trade:
        return 'border-l-emerald-400';
      case ChatChannelType.Help:
        return 'border-l-sky-400';
      case ChatChannelType.Guild:
        return 'border-l-rose-400';
      case ChatChannelType.Raid:
        return 'border-l-orange-400 bg-orange-400/5';
      case ChatChannelType.Whisper:
        return 'border-l-fuchsia-400 bg-fuchsia-400/5';
      case ChatChannelType.System:
        return isWorldSystemMessage(message)
          ? 'border-l-amber-400 bg-amber-400/5 shadow-[inset_0_0_18px_rgba(245,158,11,0.05)] hover:bg-amber-400/10'
          : 'border-l-slate-400 bg-slate-400/5';
      default:
        return 'border-l-primary';
    }
  }

  private displayChannelType(message: ChatMessageDto): ChatChannelType {
    if (message.channelType !== ChatChannelType.General) {
      return message.channelType;
    }

    if (message.contextKey === 'trade') return ChatChannelType.Trade;
    if (message.contextKey === 'help') return ChatChannelType.Help;
    return ChatChannelType.General;
  }

  isWorldAnnouncement(message: ChatMessageDto): boolean {
    return isWorldSystemMessage(message);
  }

  isInlineSystemNotice(message: ChatMessageDto): boolean {
    return isInlineChannelSystemMessage(message);
  }

  navigateToMessageTarget(message: ChatMessageDto, event?: Event): void {
    if (!message.targetUrl) return;

    event?.preventDefault();
    void this.router.navigateByUrl(message.targetUrl);
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

  trackMentionSegment(index: number): number {
    return index;
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
    if (this.isSending) return;

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
    this.isSending = true;

    try {
      const wire = parseWireCommand(body);
      if (wire.isWire) {
        if (!wire.command) {
          this.sendError = 'Usage: /wire Name Amount Cinders';
          return;
        }

        const response = await firstValueFrom(
          this.characterService.wireCinders(
            wire.command.recipientName,
            wire.command.amount,
          ),
        );
        const currentCharacter = this.characterState.currentCharacter();
        if (currentCharacter) {
          this.characterState.updateCharacter({
            ...currentCharacter,
            cinders: response.remainingCinders,
          });
        }

        this.draft = '';
        this.sendError = '';
        return;
      }

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
        case ChatChannelType.Raid:
          await this.chat.sendRaid(contextKey, body);
          break;
        default:
          return;
      }

      this.draft = '';
      this.sendError = '';
    } catch (err) {
      this.sendError = body.toLowerCase().startsWith('/wire')
        ? getWireErrorMessage(err)
        : getChatSendErrorMessage(err);
      console.warn('Unable to send chat message.', err);
    } finally {
      this.isSending = false;
    }
  }
}

export function getWireErrorMessage(error: unknown): string {
  const candidate = error as {
    errorMessage?: unknown;
    message?: unknown;
  };
  const technicalMessage =
    typeof candidate?.errorMessage === 'string'
      ? candidate.errorMessage
      : typeof candidate?.message === 'string'
        ? candidate.message
        : '';
  const normalizedMessage = technicalMessage.toLowerCase();

  if (
    normalizedMessage.includes('not enough cinders') ||
    normalizedMessage.includes('not have enough cinders')
  ) {
    return 'You do not have enough Cinders for this wire.';
  }
  if (normalizedMessage.includes('yourself')) {
    return 'You cannot wire Cinders to yourself.';
  }
  if (normalizedMessage.includes('not be found')) {
    return "That player couldn't be found.";
  }
  if (normalizedMessage.includes('at least 1')) {
    return 'The wire amount must be at least 1 Cinder.';
  }
  if (normalizedMessage.includes('only cinders')) {
    return 'Only Cinders can currently be wired.';
  }

  return "The Cinders couldn't be wired. Please try again.";
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
