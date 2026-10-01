import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  ElementRef,
  TemplateRef,
  afterRenderEffect,
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
  untracked,
  viewChild,
} from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { LG_SHELL, LgSlotDirective, lgCx, lgHasSlot } from './grimoire-core';
import { LgButtonComponent } from './button.component';
import { lgAnnounce } from './grimoire-a11y';
import { LgIconComponent } from './icon.component';

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
  /** system and loot lines are set in lore italic, without an author; a day break (D-112) is the date between two
   *  days' lines, its text only. */
  kind?: 'chat' | 'system' | 'loot' | 'day';
  /** The line mentions the player. */
  mention?: boolean;
  /** The author holds active Nobility: the crown before the name (D-066, D-112). */
  noble?: boolean;
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
  static ngTemplateContextGuard(_directive: LgChronicleTextDirective, context: unknown): context is LgChronicleTextContext {
    return true;
  }
}

function channelClass(id: string | undefined): string {
  return 'lg-ch--' + String(id || 'general').toLowerCase();
}

/**
 * Chat and the game log. Inside a GameShell it follows the shell's chat layout (docked, or a floating drawer with a
 * drag grip and a tall toggle). Live updates never move what the player is reading: the log follows the newest line
 * only while the player is at its foot; scrolled up, it keeps their place and counts what arrived. Channel settings go
 * in `lgSlot="aside"`.
 */
@Component({
  selector: 'lg-chronicle',
  imports: [NgTemplateOutlet, LgButtonComponent, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <ng-template #line let-m let-compact="compact" let-showTag="showTag"
      >@if (m.kind === 'day') {<span class="lg-chronicle__text">{{ m.text }}</span>} @else {@if (m.time && !compact) {<time class="lg-chronicle__time">{{ m.time }}</time>}@if (
        showTag && (m.channelLabel || m.channel)
      ) {<span class="lg-chronicle__tag">{{ m.channelLabel || m.channel }}</span>}@if (
        m.kind !== 'system' && m.kind !== 'loot' && author(m)
      ) {@if (m.noble) {<span class="lg-chronicle__noble" role="img" aria-label="Noble" title="Active Nobility"
            ><lg-icon name="nobility" [size]="12" /></span
          >}@if (authorActions() && !compact) {<button
            type="button"
            class="lg-chronicle__author"
            (click)="authorSelect.emit({ message: m, element: $any($event.currentTarget) })"
          >{{ author(m) }}</button>} @else {<span class="lg-chronicle__author">{{ author(m) }}</span>}}<span class="lg-chronicle__text"
        >@if (textTemplate(); as custom) {<ng-container
            [ngTemplateOutlet]="custom.template"
            [ngTemplateOutletContext]="{ $implicit: m }"
          />} @else {{{ m.text }}}</span
      >}</ng-template
    >

    <section [class]="classes()" [attr.aria-label]="label() || 'Chronicle'" (animationend)="onAnimationEnd($event)">
      <header class="lg-chronicle__head">
        @if (open()) {
          <div class="lg-chronicle__channels" role="tablist" aria-label="Channels" (keydown)="onChannelKeydown($event)">
            @for (channel of channels(); track channel.id) {
              <button
                type="button"
                role="tab"
                [attr.aria-selected]="channel.id === current()"
                [attr.tabindex]="channel.id === current() ? 0 : -1"
                [class]="channelTabClass(channel.id)"
                (click)="activeChannel.set(channel.id)"
              >{{ channel.label }}@if (channel.unread) {<span class="lg-chronicle__unread" [attr.aria-label]="channel.unread + ' unread'">{{
                    channel.unread
                  }}</span>}</button>
            }
          </div>
        } @else {
          <button
            type="button"
            [class]="tickerClass()"
            [attr.aria-label]="'Open chat' + (unreadTotal() ? ', ' + unreadTotal() + ' unread' : '')"
            (click)="open.set(true)"
          >
            @if (lastMessage(); as last) {
              <ng-container [ngTemplateOutlet]="line" [ngTemplateOutletContext]="{ $implicit: last, compact: true, showTag: true }" />
            } @else {
              <span class="lg-chronicle__text">Chronicle</span>
            }
            <span class="lg-chronicle__strip-label" aria-hidden="true">{{ label() || 'Chronicle' }}</span>
            @if (unreadTotal()) {
              <span class="lg-chronicle__unread" aria-hidden="true">{{ unreadTotal() }}</span>
            }
          </button>
        }
        @if (has('aside')) {
          <div class="lg-chronicle__aside"><ng-content select="[lgSlot=aside]" /></div>
        }
        @if (open() && isFloating()) {
          <button
            type="button"
            class="lg-chronicle__toggle lg-chronicle__tall"
            [attr.aria-pressed]="tall()"
            [attr.aria-label]="tall() ? 'Make chat shorter' : 'Make chat taller'"
            (click)="tall.set(!tall())"
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
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
            <svg viewBox="0 0 24 24" width="16" height="16" fill="currentColor" aria-hidden="true">
              <circle cx="9" cy="6" r="1.4" /><circle cx="15" cy="6" r="1.4" /><circle cx="9" cy="12" r="1.4" /><circle
                cx="15"
                cy="12"
                r="1.4"
              /><circle cx="9" cy="18" r="1.4" /><circle cx="15" cy="18" r="1.4" />
            </svg>
          </button>
        }
        @if (toggleable()) {
          <button
            type="button"
            class="lg-chronicle__toggle"
            [attr.aria-expanded]="open()"
            [attr.aria-label]="open() ? 'Collapse chat' : 'Expand chat'"
            (click)="open.set(!open())"
          >
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
              <path [attr.d]="open() ? 'M6 9l6 6 6-6' : 'M6 15l6-6 6 6'" />
            </svg>
          </button>
        }
      </header>
      @if (open()) {
        <div class="lg-chronicle__body">
          <ol
            #log
            class="lg-chronicle__log"
            [attr.aria-live]="announce() === 'all' ? 'polite' : 'off'"
            aria-relevant="additions"
            tabindex="0"
            aria-label="Messages"
            (scroll)="onLogScroll()"
          >
            @for (m of visible(); track m.id) {
              <li [attr.data-id]="m.id" [class]="lineClass(m)">
                <ng-container
                  [ngTemplateOutlet]="line"
                  [ngTemplateOutletContext]="{ $implicit: m, compact: false, showTag: current() === 'all' || m.channel !== current() }"
                />
              </li>
            }
          </ol>
          @if (newCount()) {
            <button lgButton size="sm" class="lg-chronicle__jump" [attr.aria-label]="newLabel() + ', jump to latest'" (click)="jump()">
              {{ newLabel() }}
            </button>
          }
        </div>
      }
      @if (open() && has('composer')) {
        <div class="lg-chronicle__composer lg-chronicle__composer--custom"><ng-content select="[lgSlot=composer]" /></div>
      } @else if (open() && composer()) {
        <form class="lg-chronicle__composer" (submit)="submit($event)">
          <span [class]="'lg-chronicle__prefix ' + prefixClass()">{{ composerChannelLabel() || composerChannel() || current() }}</span>
          <input
            class="lg-chronicle__input"
            [value]="draft() || ''"
            [attr.value]="draft() || ''"
            [attr.maxlength]="maxLength() ?? null"
            (input)="draft.set($any($event.target).value)"
            [attr.placeholder]="placeholder() || 'Say something — /g guild, /w name whisper'"
            aria-label="Chat message"
          />
          <button type="submit" class="lg-chronicle__send" aria-label="Send"><kbd class="lg-key">↵</kbd></button>
        </form>
      }
    </section>
  `,
})
export class LgChronicleComponent {
  readonly channels = input.required<readonly LgChronicleChannel[]>();
  readonly messages = input.required<readonly LgChronicleMessage[]>();
  readonly activeChannel = model<string>();
  readonly open = model(true);
  /** Show the collapse and expand button (React's onToggle). */
  readonly toggleable = input(true, { transform: booleanAttribute });
  /**
   * What screen readers hear: 'all' makes the log a polite live region; 'mentions' announces only lines that mention
   * the player and whispers to them, through the throttled announcer; 'off' announces nothing.
   */
  readonly announce = input<'all' | 'mentions' | 'off'>('all');
  /** Show the composer. */
  readonly composer = input(true, { transform: booleanAttribute });
  /** The composer's text; two-way bindable. */
  readonly draft = model('');
  readonly composerChannel = input<string>();
  readonly composerChannelLabel = input<string>();
  readonly placeholder = input('Say something — /g guild, /w name whisper');
  readonly maxLength = input<number>();
  /** Force the floating look; leave unset to follow the shell's chat layout. */
  readonly floating = input<boolean | undefined, unknown>(undefined, {
    transform: (value: unknown) => (value === undefined || value === null ? undefined : booleanAttribute(value)),
  });
  readonly label = input('Chronicle');
  /** Emits the draft when it is not empty; clear `draft` when the message is accepted. */
  readonly send = output<string>();
  /** Makes the author a button that emits `authorSelect` to open the host's player actions (D-112). Not in the ticker. */
  readonly authorActions = input(false, { transform: booleanAttribute });
  readonly authorSelect = output<{ message: LgChronicleMessage; element: HTMLElement }>();

  protected readonly shell = inject(LG_SHELL, { optional: true });
  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly textTemplate = contentChild(LgChronicleTextDirective);
  private readonly log = viewChild<ElementRef<HTMLElement>>('log');

  protected readonly tall = signal(false);
  protected readonly newCount = signal(0);
  private readonly entering = signal(false);
  private stick = true;
  private anchor: { id: string; off: number } | null = null;
  private seen: { last: string | number | null; active: string; open: boolean };
  private seenCount: number;

  protected readonly isFloating = computed(() =>
    this.floating() !== undefined ? this.floating() === true : this.shell?.chatLayout() === 'floating',
  );
  protected readonly current = computed(() => this.activeChannel() || this.channels()[0]?.id || 'all');
  protected readonly visible = computed(() => {
    const c = this.current();
    const all = this.messages() || [];
    return c === 'all' ? all : all.filter((m) => m.channel === c);
  });
  protected readonly lastMessage = computed(() => {
    const all = this.messages() || [];
    return all.length ? all[all.length - 1] : null;
  });
  protected readonly unreadTotal = computed(() => this.channels().reduce((a, c) => a + (c.unread || 0), 0));
  protected readonly newLabel = computed(() => this.newCount() + ' new ' + (this.newCount() === 1 ? 'line' : 'lines'));
  protected readonly classes = computed(() =>
    lgCx(
      'lg-chronicle',
      this.open() ? 'is-open' : 'is-collapsed',
      this.isFloating() && 'lg-chronicle--floating',
      this.isFloating() && this.tall() && 'is-tall',
      this.entering() && 'is-entering',
    ),
  );
  protected readonly tickerClass = computed(() => {
    const last = this.lastMessage();
    return lgCx('lg-chronicle__ticker', last && channelClass(last.channel), last && 'lg-chronicle__line--' + (last.kind || 'chat'));
  });
  protected readonly prefixClass = computed(() => channelClass(this.composerChannel() || this.current()));

  constructor() {
    this.seen = { last: null, active: '', open: true };
    this.seenCount = 0;
    effect(() => this.shell?.setChatCollapsed(!this.open()));

    // Spatial: opened by the player, the body rises in over duration-base; closing is at once.
    let wasOpen: boolean | null = null;
    effect(() => {
      const open = this.open();
      if (open && wasOpen === false) this.entering.set(true);
      wasOpen = open;
    });

    // Keep the reader's place; follow the foot only while the reader is there.
    afterRenderEffect(() => {
      const msgs = this.visible();
      const active = this.current();
      const open = this.open();
      const el = this.log()?.nativeElement;
      untracked(() => {
        const lastId = msgs.length ? msgs[msgs.length - 1].id : null;
        const sn = this.seen;
        const reset = sn.active !== active || (open && !sn.open);
        const prevLast = sn.last;
        this.seen = { last: lastId, active, open };
        if (!el) return;
        if (reset || this.stick) {
          this.stick = true;
          el.scrollTop = el.scrollHeight;
          if (this.newCount()) this.newCount.set(0);
          return;
        }
        if (prevLast === lastId) return;
        const a = this.anchor;
        const lineEl = a && el.querySelector<HTMLElement>(`.lg-chronicle__line[data-id="${String(a.id).replace(/"/g, '\\"')}"]`);
        if (a && lineEl) el.scrollTop = lineEl.offsetTop - a.off;
        let idx = -1;
        for (let i = msgs.length - 1; i >= 0; i--)
          if (msgs[i].id === prevLast) {
            idx = i;
            break;
          }
        const added = idx < 0 ? msgs.length : msgs.length - 1 - idx;
        if (added > 0) this.newCount.update((n) => n + added);
      });
    });

    // 'mentions': announce lines that mention the player, and whispers to them — only lines that arrive later.
    let first = true;
    effect(() => {
      const all = this.messages() || [];
      const mode = this.announce();
      if (first) {
        first = false;
        this.seenCount = all.length;
        return;
      }
      const from = this.seenCount;
      this.seenCount = all.length;
      if (mode !== 'mentions' || from >= all.length) return;
      all.slice(from).forEach((m) => {
        if (!(m.mention || m.direction === 'from')) return;
        const who =
          m.direction === 'from' ? 'Whisper from ' + m.author : m.author ? m.author + ' mentioned you' : 'You were mentioned';
        lgAnnounce(typeof m.text === 'string' ? who + ': ' + m.text : who, { key: 'chronicle-' + (m.id ?? who) });
      });
    });
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }

  protected author(m: LgChronicleMessage): string {
    if (m.direction === 'to') return 'To ' + m.author;
    if (m.direction === 'from') return 'From ' + m.author;
    return m.author || '';
  }

  protected channelTabClass(id: string): string {
    return lgCx('lg-chronicle__channel', channelClass(id), id === this.current() && 'is-active');
  }

  protected lineClass(m: LgChronicleMessage): string {
    return lgCx('lg-chronicle__line', 'lg-chronicle__line--' + (m.kind || 'chat'), channelClass(m.channel), m.mention && 'is-mention');
  }

  protected onLogScroll(): void {
    const el = this.log()?.nativeElement;
    if (!el) return;
    const atFoot = el.scrollHeight - el.scrollTop - el.clientHeight < 24;
    this.stick = atFoot;
    const lines = el.querySelectorAll<HTMLElement>('.lg-chronicle__line');
    this.anchor = null;
    for (const line of Array.from(lines)) {
      if (line.offsetTop + line.offsetHeight > el.scrollTop) {
        this.anchor = { id: line.getAttribute('data-id')!, off: line.offsetTop - el.scrollTop };
        break;
      }
    }
    if (atFoot && this.newCount()) this.newCount.set(0);
  }

  protected jump(): void {
    const el = this.log()?.nativeElement;
    this.stick = true;
    this.newCount.set(0);
    if (el) {
      el.scrollTop = el.scrollHeight;
      el.focus();
    }
  }

  protected onAnimationEnd(event: AnimationEvent): void {
    if (this.entering() && (event.target as Element).classList?.contains('lg-chronicle__body')) this.entering.set(false);
  }

  protected submit(event: Event): void {
    event.preventDefault();
    this.stick = true;
    const text = this.draft();
    if (text && String(text).trim()) this.send.emit(text);
  }

  protected onChannelKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    const channels = this.channels();
    if (!channels.length) return;
    event.preventDefault();
    const i = channels.findIndex((c) => c.id === this.current());
    const j = (i + (event.key === 'ArrowRight' ? 1 : -1) + channels.length) % channels.length;
    this.activeChannel.set(channels[j].id);
    (event.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('[role=tab]')[j]?.focus();
  }
}
