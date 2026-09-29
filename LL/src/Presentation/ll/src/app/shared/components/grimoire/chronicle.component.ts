import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  ElementRef,
  Injector,
  TemplateRef,
  afterNextRender,
  booleanAttribute,
  computed,
  contentChild,
  contentChildren,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_SHELL, LgSlotDirective, lgHasSlot } from './grimoire-core';

export interface LgChronicleChannel {
  /** 'all' shows the merged feed of visible channels. */
  id: string;
  label: string;
  unread?: number;
}

export interface LgChronicleMessage {
  id: string | number;
  /** general, trade, help, guild, whisper, raid, invites, system, loot */
  channel: string;
  channelLabel?: string;
  time?: string;
  author?: string;
  /** Whispers: "From Kaelen" / "To Kaelen". */
  direction?: 'from' | 'to';
  /** system and loot lines are set in lore italic, without an author. */
  kind?: 'chat' | 'system' | 'loot';
  /** The line mentions the player. */
  mention?: boolean;
  text: string;
}

export interface LgChronicleTextContext {
  $implicit: LgChronicleMessage;
}

/**
 * Custom rendering for a message's text (mentions, item links):
 * `<ng-template lgChronicleText let-message>…</ng-template>`
 */
@Directive({ selector: 'ng-template[lgChronicleText]' })
export class LgChronicleTextDirective {
  readonly template = inject<TemplateRef<LgChronicleTextContext>>(TemplateRef);
  static ngTemplateContextGuard(
    _directive: LgChronicleTextDirective,
    context: unknown,
  ): context is LgChronicleTextContext {
    return true;
  }
}

/**
 * The Chronicle: chat and the game log in one place. Inside a GameShell it follows
 * the shell's chat layout (docked or floating drawer, with drag grip and tall toggle).
 * Channel settings go in `lgSlot="aside"`.
 */
@Component({
  selector: 'lg-chronicle',
  imports: [NgTemplateOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    role: 'region',
    '[attr.aria-label]': 'label()',
  },
  template: `
    <ng-template #line let-message let-compact="compact" let-showTag="showTag">
      @if (message.time && !compact) {
        <time class="lg-chronicle__time">{{ message.time }}</time>
      }
      @if (showTag && (message.channelLabel || message.channel)) {
        <span class="lg-chronicle__tag">{{ message.channelLabel || message.channel }}</span>
      }
      @if (message.kind !== 'system' && message.kind !== 'loot' && message.author) {
        <span class="lg-chronicle__author">{{ authorLabel(message) }}</span>
      }
      <span class="lg-chronicle__text">
        @if (textTemplate(); as custom) {
          <ng-container [ngTemplateOutlet]="custom.template" [ngTemplateOutletContext]="{ $implicit: message }" />
        } @else {
          {{ message.text }}
        }
      </span>
    </ng-template>

    <header class="lg-chronicle__head">
      @if (open()) {
        <div class="lg-chronicle__channels" role="tablist" aria-label="Channels" (keydown)="onChannelKeydown($event)">
          @for (channel of channels(); track channel.id) {
            <button
              type="button"
              role="tab"
              class="lg-chronicle__channel"
              [class]="'lg-ch--' + channel.id"
              [class.is-active]="channel.id === currentChannel()"
              [attr.aria-selected]="channel.id === currentChannel()"
              [attr.tabindex]="channel.id === currentChannel() ? 0 : -1"
              (click)="activeChannel.set(channel.id)"
            >
              {{ channel.label }}
              @if (channel.unread) {
                <span class="lg-chronicle__unread" [attr.aria-label]="channel.unread + ' unread'">{{ channel.unread }}</span>
              }
            </button>
          }
        </div>
      } @else {
        <button
          type="button"
          class="lg-chronicle__ticker"
          [class]="lastMessageClass()"
          [attr.aria-label]="'Open chat' + (unreadTotal() ? ', ' + unreadTotal() + ' unread' : '')"
          (click)="open.set(true)"
        >
          @if (lastMessage(); as last) {
            <ng-container [ngTemplateOutlet]="line" [ngTemplateOutletContext]="{ $implicit: last, compact: true, showTag: true }" />
          } @else {
            <span class="lg-chronicle__text">{{ label() }}</span>
          }
          <span class="lg-chronicle__strip-label" aria-hidden="true">{{ label() }}</span>
          @if (unreadTotal()) {
            <span class="lg-chronicle__unread" aria-hidden="true">{{ unreadTotal() }}</span>
          }
        </button>
      }
      @if (has('aside')) {
        <div class="lg-chronicle__aside"><ng-content select="[lgSlot=aside]" /></div>
      }
      @if (isFloating() && open()) {
        <button
          type="button"
          class="lg-chronicle__toggle lg-chronicle__tall"
          [attr.aria-pressed]="tall()"
          [attr.aria-label]="tall() ? 'Make chat shorter' : 'Make chat taller'"
          (click)="tall.set(!tall())"
        >
          <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.6"
            stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path [attr.d]="tall() ? 'M8 4l4 4 4-4M8 20l4-4 4 4' : 'M8 8l4-4 4 4M8 16l4 4 4-4'" />
          </svg>
        </button>
      }
      @if (isFloating() && shell) {
        <button
          type="button"
          class="lg-chronicle__toggle lg-chronicle__grip"
          aria-label="Drag chat"
          title="Drag to move · arrow keys nudge"
          (pointerdown)="shell.startChronicleDrag($event)"
          (keydown)="shell.nudgeChronicle($event)"
        >
          <svg viewBox="0 0 24 24" width="18" height="18" fill="currentColor" aria-hidden="true">
            <circle cx="9" cy="6" r="1.4" /><circle cx="15" cy="6" r="1.4" />
            <circle cx="9" cy="12" r="1.4" /><circle cx="15" cy="12" r="1.4" />
            <circle cx="9" cy="18" r="1.4" /><circle cx="15" cy="18" r="1.4" />
          </svg>
        </button>
      }
      <button
        type="button"
        class="lg-chronicle__toggle"
        [attr.aria-expanded]="open()"
        [attr.aria-label]="open() ? 'Collapse chat' : 'Expand chat'"
        (click)="open.set(!open())"
      >
        <svg viewBox="0 0 24 24" width="18" height="18" fill="none" stroke="currentColor" stroke-width="1.6"
          stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <path [attr.d]="open() ? 'M6 9l6 6 6-6' : 'M6 15l6-6 6 6'" />
        </svg>
      </button>
    </header>

    @if (open()) {
      <ol #log class="lg-chronicle__log" aria-live="polite" aria-relevant="additions" tabindex="0" aria-label="Messages">
        @for (message of visibleMessages(); track message.id) {
          <li
            class="lg-chronicle__line"
            [class]="'lg-chronicle__line--' + (message.kind ?? 'chat') + ' lg-ch--' + message.channel"
            [class.is-mention]="!!message.mention"
          >
            <ng-container
              [ngTemplateOutlet]="line"
              [ngTemplateOutletContext]="{ $implicit: message, compact: false, showTag: currentChannel() === 'all' || message.channel !== currentChannel() }"
            />
          </li>
        }
      </ol>
      <form class="lg-chronicle__composer" (submit)="submit($event)">
        <span class="lg-chronicle__prefix" [class]="'lg-ch--' + (composerChannel() ?? currentChannel())">
          {{ composerChannelLabel() ?? composerChannel() ?? currentChannel() }}
        </span>
        <input
          class="lg-chronicle__input"
          aria-label="Chat message"
          [attr.maxlength]="maxLength() ?? null"
          [attr.placeholder]="placeholder()"
          [value]="draft()"
          (input)="draft.set($any($event.target).value)"
        />
        <button type="submit" class="lg-chronicle__send" aria-label="Send"><kbd class="lg-key">↵</kbd></button>
      </form>
    }
  `,
})
export class LgChronicleComponent {
  readonly channels = input.required<readonly LgChronicleChannel[]>();
  readonly messages = input.required<readonly LgChronicleMessage[]>();
  readonly activeChannel = model<string>();
  readonly open = model(true);
  /** The composer's text; two-way bindable. */
  readonly draft = model('');
  readonly composerChannel = input<string>();
  readonly composerChannelLabel = input<string>();
  readonly placeholder = input('Say something — /g guild, /w name whisper');
  readonly maxLength = input<number>();
  /** Force drawer styling outside a GameShell. */
  /** Force the floating look; leave unset to follow the shell's chat layout. */
  readonly floating = input<boolean | undefined, unknown>(undefined, {
    transform: (value: unknown) =>
      value === undefined || value === null ? undefined : booleanAttribute(value),
  });
  readonly label = input('Chronicle');
  /** Emits the trimmed draft; clear `draft` when the message is accepted. */
  readonly send = output<string>();

  protected readonly shell = inject(LG_SHELL, { optional: true });
  private readonly injector = inject(Injector);
  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly textTemplate = contentChild(LgChronicleTextDirective);
  private readonly log = viewChild<ElementRef<HTMLElement>>('log');

  protected readonly tall = signal(false);

  protected readonly isFloating = computed(() =>
    this.floating() !== undefined
      ? this.floating() === true
      : this.shell?.chatLayout() === 'floating',
  );
  protected readonly currentChannel = computed(
    () => this.activeChannel() ?? this.channels()[0]?.id ?? 'all',
  );
  protected readonly visibleMessages = computed(() => {
    const channel = this.currentChannel();
    const all = this.messages();
    return channel === 'all' ? all : all.filter((message) => message.channel === channel);
  });
  protected readonly lastMessage = computed(() => {
    const all = this.messages();
    return all.length ? all[all.length - 1] : null;
  });
  protected readonly lastMessageClass = computed(() => {
    const last = this.lastMessage();
    return last
      ? `lg-chronicle__ticker lg-ch--${last.channel} lg-chronicle__line--${last.kind ?? 'chat'}`
      : 'lg-chronicle__ticker';
  });
  protected readonly unreadTotal = computed(() =>
    this.channels().reduce((sum, channel) => sum + (channel.unread ?? 0), 0),
  );
  protected readonly hostClass = computed(() =>
    [
      'lg-chronicle',
      this.open() ? 'is-open' : 'is-collapsed',
      this.isFloating() ? 'lg-chronicle--floating' : '',
      this.isFloating() && this.tall() ? 'is-tall' : '',
    ]
      .filter(Boolean)
      .join(' '),
  );

  constructor() {
    effect(() => {
      this.shell?.setChatCollapsed(!this.open());
    });
    effect(() => {
      this.visibleMessages();
      this.open();
      afterNextRender(
        () => {
          const element = this.log()?.nativeElement;
          if (element) element.scrollTop = element.scrollHeight;
        },
        { injector: this.injector },
      );
    });
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }

  protected authorLabel(message: LgChronicleMessage): string {
    if (message.direction === 'to') return `To ${message.author}`;
    if (message.direction === 'from') return `From ${message.author}`;
    return message.author ?? '';
  }

  protected submit(event: Event): void {
    event.preventDefault();
    const text = this.draft().trim();
    if (text) this.send.emit(text);
  }

  protected onChannelKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    const channels = this.channels();
    if (!channels.length) return;
    event.preventDefault();
    const current = channels.findIndex((channel) => channel.id === this.currentChannel());
    const next = (current + (event.key === 'ArrowRight' ? 1 : -1) + channels.length) % channels.length;
    this.activeChannel.set(channels[next].id);
    const tabs = (event.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('[role=tab]');
    tabs[next]?.focus();
  }
}
