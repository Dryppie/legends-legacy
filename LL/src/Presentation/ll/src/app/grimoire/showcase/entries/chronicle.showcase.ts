import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgChronicleChannel,
  LgChronicleComponent,
  LgChronicleMessage,
  LgChronicleTextDirective,
  LgItemLinkComponent,
  LgKeyComponent,
  LgRarity,
  LgSlotDirective,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

const CHANNELS: readonly LgChronicleChannel[] = [
  { id: 'all', label: 'All' },
  { id: 'general', label: 'General' },
  { id: 'trade', label: 'Trade' },
  { id: 'help', label: 'Help' },
  { id: 'guild', label: 'Guild', unread: 3 },
  { id: 'whisper', label: 'Whispers', unread: 1 },
  { id: 'raid', label: 'Raid' },
  { id: 'loot', label: 'Loot' },
];

const MESSAGES: readonly LgChronicleMessage[] = [
  {
    id: 1,
    channel: 'general',
    channelLabel: 'General',
    time: '21:02',
    author: 'Maren',
    text: 'Anyone seen the Ember Knight near Shenic tonight?',
  },
  {
    id: 2,
    channel: 'system',
    channelLabel: 'System',
    kind: 'system',
    time: '21:03',
    text: 'The World Tower resets in 2 hours.',
  },
  {
    id: 3,
    channel: 'trade',
    channelLabel: 'Trade',
    time: '21:04',
    author: 'Oswin',
    text: 'WTS [Ember Wolf Essence] ×3 — 400 Cinders each',
  },
  {
    id: 4,
    channel: 'guild',
    channelLabel: 'Guild',
    time: '21:05',
    author: 'Kaelen',
    text: 'Raid at 22:00. Bring resist gear, the boss burns.',
  },
  {
    id: 5,
    channel: 'loot',
    channelLabel: 'Loot',
    kind: 'loot',
    time: '21:06',
    text: 'You found [Soul Prism] and 120 Cinders.',
  },
  {
    id: 6,
    channel: 'general',
    channelLabel: 'General',
    time: '21:07',
    author: 'Tamsin',
    mention: true,
    text: '@Aldric that crown is ridiculous. Where from?',
  },
  {
    id: 7,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '21:08',
    author: 'Kaelen',
    direction: 'from',
    text: 'Can you tank tonight?',
  },
  {
    id: 8,
    channel: 'help',
    channelLabel: 'Help',
    time: '21:09',
    author: 'Pip',
    text: 'How do Essences drop? Is it per kill?',
  },
];

const TRADE_MESSAGES: readonly LgChronicleMessage[] = [
  {
    id: 't1',
    channel: 'trade',
    channelLabel: 'Trade',
    time: '20:51',
    author: 'Maren',
    text: 'WTB Iron Ore ×200, paying 2 Cinders each',
  },
  {
    id: 't2',
    channel: 'general',
    channelLabel: 'General',
    time: '20:55',
    author: 'Pip',
    text: 'Is the Bazaar down for anyone else?',
  },
  {
    id: 't3',
    channel: 'trade',
    channelLabel: 'Trade',
    time: '20:58',
    author: 'Pip',
    text: 'Selling Wolf Pelt ×40, whisper me',
  },
  ...MESSAGES,
  {
    id: 't4',
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '21:10',
    author: 'Oswin',
    direction: 'to',
    text: 'Still selling? I can do 1,100 for all three.',
  },
];

const NOBLE_MESSAGES: readonly LgChronicleMessage[] = [
  {
    id: 1,
    channel: 'general',
    channelLabel: 'General',
    time: '21:02',
    author: 'Maren',
    noble: true,
    text: 'Anyone for the tower?',
  },
  {
    id: 2,
    channel: 'guild',
    channelLabel: 'Guild',
    time: '21:05',
    author: 'Kaelen',
    text: 'Raid at 22:00. Bring resist gear, the boss burns.',
  },
  {
    id: 3,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '21:08',
    author: 'Kaelen',
    direction: 'from',
    text: 'Can you tank tonight?',
  },
  {
    id: 4,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '21:09',
    author: 'Kaelen',
    direction: 'to',
    text: 'Yes, see you at 22:00.',
  },
  {
    id: 5,
    channel: 'trade',
    channelLabel: 'Trade',
    time: '21:11',
    author: 'Oswin',
    noble: true,
    text: 'WTS Ember Wolf Essence ×3',
  },
];

const DAY_MESSAGES: readonly LgChronicleMessage[] = [
  { id: 'd1', channel: 'system', kind: 'day', text: '30 Sep 2026' },
  {
    id: 1,
    channel: 'general',
    channelLabel: 'General',
    time: '23:52',
    author: 'Maren',
    text: 'Night all, raid tomorrow at 22:00.',
  },
  {
    id: 2,
    channel: 'guild',
    channelLabel: 'Guild',
    time: '23:58',
    author: 'Kaelen',
    text: 'Sleep well.',
  },
  { id: 'd2', channel: 'system', kind: 'day', text: '1 Oct 2026' },
  {
    id: 3,
    channel: 'loot',
    channelLabel: 'Loot',
    kind: 'loot',
    time: '00:04',
    text: 'You found 2 Wolf Pelt.',
  },
  {
    id: 4,
    channel: 'system',
    channelLabel: 'System',
    kind: 'system',
    time: '00:10',
    text: 'The World Tower has reset.',
  },
];

interface RichPart {
  text?: string;
  item?: string;
  rarity?: LgRarity;
  mention?: string;
}

/** What the custom text template draws for a message: item links in their rarity colour and the @mention in bold. */
const RICH_PARTS: Record<string, readonly RichPart[]> = {
  '3': [
    { text: 'WTS ' },
    { item: 'Ember Wolf Essence', rarity: 'Rare' },
    { text: ' ×3 — 400 Cinders each' },
  ],
  '5': [
    { text: 'You found ' },
    { item: 'Soul Prism', rarity: 'Epic' },
    { text: ' and 120 Cinders.' },
  ],
  '6': [
    { mention: '@Aldric' },
    { text: ' that crown is ridiculous. Where from?' },
  ],
};

@Component({
  selector: 'sc-chronicle-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgChronicleComponent,
    LgChronicleTextDirective,
    LgItemLinkComponent,
    LgKeyComponent,
    LgSlotDirective,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Channels"
      notes="The All feed: every line carries its channel's tag; a channel's hue marks only its tag and speaker names. Pick a channel, collapse it, or send a line."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="messages()"
        [(activeChannel)]="channel"
        [(open)]="open"
        [(draft)]="draft"
        composerChannel="general"
        composerChannelLabel="General"
        (send)="send($event)"
      />
    </ng-template>

    <ng-template
      scStory="Collapsed"
      notes="A one-line ticker with the newest line and the unread total; it opens the Chronicle."
      width="24rem"
      height="2.75rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="allMessages"
        [open]="false"
        [composer]="false"
      />
    </ng-template>

    <ng-template
      scStory="Trade channel"
      notes="One channel: lines from it drop their tag, and Trade's amber marks its speakers and the composer's prefix."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="tradeMessages"
        activeChannel="trade"
        announce="mentions"
        composerChannel="trade"
        composerChannelLabel="Trade"
        [toggleable]="false"
      />
    </ng-template>

    <ng-template
      scStory="Author actions"
      notes="Each author is a button that opens the host's player actions; a Noble author has the crown before the name."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="nobleMessages"
        authorActions
        [toggleable]="false"
        [composer]="false"
      />
    </ng-template>

    <ng-template
      scStory="Day breaks"
      notes="The date between two days' lines, in label capitals between two rules, with no time, tag or author."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="dayMessages"
        activeChannel="all"
        [toggleable]="false"
        [composer]="false"
      />
    </ng-template>

    <ng-template
      scStory="Custom text"
      notes="An ng-template lgChronicleText draws each line's text: item links in their rarity colour, the @mention in bold."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="allMessages"
        activeChannel="all"
        [toggleable]="false"
        [composer]="false"
      >
        <!-- Whitespace between these parts is rendered, so Prettier must leave them on one line. -->
        <!-- prettier-ignore -->
        <ng-template lgChronicleText let-message
          >@for (part of parts(message); track $index) {@if (part.item) {<button lgItemLink [rarity]="part.rarity">{{
              part.item
            }}</button>} @else if (part.mention) {<span class="lg-mention">{{ part.mention }}</span>} @else {{{ part.text }}}}</ng-template
        >
      </lg-chronicle>
    </ng-template>

    <ng-template
      scStory="Custom composer"
      notes="A composer of the host's own takes the built-in one's place and may use its parts: the row, the prefix, the input, the send key and a note."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="allMessages"
        activeChannel="guild"
        [toggleable]="false"
      >
        <div lgSlot="composer">
          <div class="lg-chronicle__row">
            <span class="lg-chronicle__prefix lg-ch--guild">Guild</span>
            <div
              class="lg-chronicle__input"
              contenteditable="true"
              role="textbox"
              aria-label="Chat message"
              data-placeholder="Say something — @ to mention a player"
            ></div>
            <button type="button" class="lg-chronicle__send" aria-label="Send">
              <lg-key>↵</lg-key>
            </button>
          </div>
          <p class="lg-chronicle__note">
            Guild chat is seen by your guild only.
          </p>
        </div>
      </lg-chronicle>
    </ng-template>

    <ng-template
      scStory="Channel settings"
      notes="Channel settings and the online count go in the aside, beside the channel tabs."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="allMessages"
        [composer]="false"
      >
        <div lgSlot="aside" class="sc-row">
          <span class="lg-chronicle__online">128 online</span>
          <button
            type="button"
            class="lg-chronicle__toggle"
            aria-label="Choose visible chat channels"
            title="Choose visible channels"
          >
            <svg
              viewBox="0 0 24 24"
              width="16"
              height="16"
              fill="none"
              stroke="currentColor"
              stroke-width="1.6"
              stroke-linecap="round"
              aria-hidden="true"
            >
              <path d="M4 7h10M18 7h2M4 17h2M10 17h10" />
              <circle cx="16" cy="7" r="2" />
              <circle cx="8" cy="17" r="2" />
            </svg>
          </button>
        </div>
      </lg-chronicle>
    </ng-template>

    <ng-template
      scStory="Empty"
      notes="No lines yet: the channels and the composer, over an empty log."
      width="24rem"
      height="20rem"
      flush
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="[]"
        composerChannel="general"
        composerChannelLabel="General"
      />
    </ng-template>

    <ng-template
      scStory="Floating"
      notes="The floating drawer's look outside a GameShell: an opaque Level 1 surface with the tall toggle. The shell adds the drag grip."
      width="25rem"
      height="31rem"
    >
      <lg-chronicle
        [channels]="channels"
        [messages]="allMessages"
        [floating]="true"
        [composer]="false"
      />
    </ng-template>
  `,
})
export class ChronicleShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly channels = CHANNELS;
  protected readonly allMessages = MESSAGES;
  protected readonly tradeMessages = TRADE_MESSAGES;
  protected readonly nobleMessages = NOBLE_MESSAGES;
  protected readonly dayMessages = DAY_MESSAGES;

  protected readonly channel = signal('all');
  protected readonly open = signal(true);
  protected readonly draft = signal('');
  protected readonly messages = signal<readonly LgChronicleMessage[]>(MESSAGES);
  private sent = 0;

  protected send(text: string): void {
    this.sent += 1;
    this.messages.update((list) => [
      ...list,
      {
        id: 'sent-' + this.sent,
        channel: 'general',
        channelLabel: 'General',
        time: '21:10',
        author: 'Aldric',
        text,
      },
    ]);
    this.draft.set('');
  }

  protected parts(message: LgChronicleMessage): readonly RichPart[] {
    return RICH_PARTS[String(message.id)] ?? [{ text: message.text }];
  }
}

export const CHRONICLE_SHOWCASE: ShowcaseEntry = {
  slug: 'chronicle',
  name: 'Chronicle',
  tier: 'shell',
  summary: 'Chat and the game log.',
  covers: ['LgChronicleComponent', 'LgChronicleTextDirective'],
  readme: 'src/app/grimoire/shell/chronicle/README.md',
  component: ChronicleShowcaseComponent,
};
