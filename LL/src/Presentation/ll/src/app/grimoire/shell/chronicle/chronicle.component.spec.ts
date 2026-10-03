import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestKey } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgChronicleHarness } from '../../testing/chronicle.harness';
import { lgWatchAnnouncements } from '../../testing/announcer';
import {
  LgChronicleAsideComponent,
  LgChronicleChannel,
  LgChronicleComponent,
  LgChronicleComposerComponent,
  LgChronicleMessage,
  LgChronicleMessageDirective,
} from './chronicle.component';

// The data the parity cases used (D-130), before the parity check was retired.
const CHANNELS: LgChronicleChannel[] = [
  { id: 'all', label: 'All' },
  { id: 'general', label: 'General', unread: 2 },
  { id: 'trade', label: 'Trade' },
];
const MESSAGES: LgChronicleMessage[] = [
  {
    id: 1,
    channel: 'general',
    time: '12:01',
    author: 'Maren',
    text: 'Anyone for the tower?',
  },
  {
    id: 2,
    channel: 'system',
    kind: 'system',
    time: '12:02',
    text: 'The Colosseum opens in 5 minutes.',
  },
  {
    id: 3,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '12:03',
    author: 'Kaelen',
    direction: 'from',
    text: 'Trade?',
    mention: true,
  },
  {
    id: 4,
    channel: 'trade',
    time: '12:04',
    author: 'You',
    text: 'WTS Ember Fang',
  },
];
const MESSAGES_NOBLE: LgChronicleMessage[] = [
  {
    id: 1,
    channel: 'general',
    time: '12:01',
    author: 'Maren',
    noble: true,
    text: 'Anyone for the tower?',
  },
  {
    id: 2,
    channel: 'whisper',
    channelLabel: 'Whisper',
    time: '12:03',
    author: 'Kaelen',
    direction: 'from',
    text: 'Trade?',
  },
];

// The parity case i-chronicle-author.
@Component({
  imports: [LgChronicleComponent],
  template: `
    <lg-chronicle
      [channels]="channels"
      [messages]="messages"
      authorActions
      [toggleable]="false"
      [composer]="false"
      (authorSelect)="selected.push($event)"
    />
  `,
})
class AuthorHost {
  readonly channels = CHANNELS;
  readonly messages = MESSAGES_NOBLE;
  readonly selected: { message: LgChronicleMessage; element: HTMLElement }[] =
    [];
}

// The parity case i-chronicle.
@Component({
  imports: [LgChronicleComponent],
  template: `
    <lg-chronicle
      [channels]="channels"
      [messages]="messages"
      [(activeChannel)]="channel"
      [(open)]="open"
      [composer]="false"
    />
  `,
})
class ChannelsHost {
  readonly channels = CHANNELS;
  readonly messages = MESSAGES;
  readonly channel = signal('all');
  readonly open = signal(true);
}

// The parity case i-chronicle-live: thirty lines in a log too short to show them all.
@Component({
  imports: [LgChronicleComponent],
  template: `
    <div style="height: 260px; display: flex; flex-direction: column">
      <lg-chronicle
        [channels]="channels"
        [messages]="lines()"
        activeChannel="all"
        [toggleable]="false"
        [composer]="false"
      />
    </div>
  `,
})
class LiveHost {
  readonly channels = CHANNELS;
  readonly lines = signal<LgChronicleMessage[]>(
    Array.from({ length: 30 }, (_, i) => ({
      id: 'n' + (i + 1),
      channel: 'general',
      author: 'Bot',
      text: 'Line ' + (i + 1),
    })),
  );
  addLine(): void {
    const n = this.lines().length + 1;
    this.lines.set([
      ...this.lines(),
      { id: 'n' + n, channel: 'general', author: 'Bot', text: 'Line ' + n },
    ]);
  }
}

/** Lets the browser render a frame (and send the scroll events it owes) before the next check. */
const nextFrame = () =>
  new Promise<void>((resolve) =>
    requestAnimationFrame(() => setTimeout(resolve)),
  );

// A Chronicle with its regions and a message template, as the game's chat composes it.
@Component({
  imports: [
    LgChronicleComponent,
    LgChronicleAsideComponent,
    LgChronicleComposerComponent,
    LgChronicleMessageDirective,
  ],
  template: `
    <lg-chronicle
      [channels]="channels"
      [messages]="messages"
      [open]="open()"
      label="Chat"
    >
      <ng-template lgChronicleMessage let-message
        ><b class="sc-text">{{ message.text.toUpperCase() }}</b></ng-template
      >
      <lg-chronicle-aside
        ><span class="sc-online">12 online</span></lg-chronicle-aside
      >
      <lg-chronicle-composer
        ><input class="sc-own" aria-label="Own composer"
      /></lg-chronicle-composer>
    </lg-chronicle>
  `,
})
class ComposedChronicleHost {
  readonly channels = CHANNELS;
  readonly messages = MESSAGES;
  readonly open = signal(true);
}

describe('LgChronicleComponent', () => {
  describe('regions and the message template', () => {
    let fixture: ComponentFixture<ComposedChronicleHost>;
    let chronicle: LgChronicleHarness;

    beforeEach(async () => {
      fixture = TestBed.createComponent(ComposedChronicleHost);
      chronicle =
        await TestbedHarnessEnvironment.loader(fixture).getHarness(
          LgChronicleHarness,
        );
    });

    it('is a region named by its label', async () => {
      const host: HTMLElement =
        fixture.nativeElement.querySelector('lg-chronicle');
      expect(host.getAttribute('role')).toBe('region');
      expect(await chronicle.getLabel()).toBe('Chat');
    });

    it('draws each line’s text with the template, given the message', () => {
      const texts = Array.from(
        fixture.nativeElement.querySelectorAll('.lg-chronicle__text .sc-text'),
        (el: Element) => el.textContent,
      );
      expect(texts).toEqual(MESSAGES.map((m) => m.text.toUpperCase()));
    });

    it('puts the aside in the head and the composer in the built-in one’s place', () => {
      const el: HTMLElement = fixture.nativeElement;
      expect(
        el.querySelector('.lg-chronicle__head > lg-chronicle-aside .sc-online'),
      ).not.toBeNull();
      expect(
        el.querySelector('lg-chronicle > lg-chronicle-composer .sc-own'),
      ).not.toBeNull();
      expect(el.querySelector('form.lg-chronicle__composer')).toBeNull();
    });

    it('collapsed, it keeps the aside and drops the composer', async () => {
      fixture.componentInstance.open.set(false);
      fixture.detectChanges();
      const el: HTMLElement = fixture.nativeElement;
      expect(el.querySelector('lg-chronicle-aside')?.classList).toContain(
        'is-collapsed',
      );
      expect(el.querySelector('.sc-own')?.isConnected).toBeFalsy();
      expect(await chronicle.isOpen()).toBeFalse();
    });
  });

  describe('author actions (i-chronicle-author)', () => {
    let fixture: ComponentFixture<AuthorHost>;
    let chronicle: LgChronicleHarness;

    beforeEach(async () => {
      fixture = TestBed.createComponent(AuthorHost);
      chronicle =
        await TestbedHarnessEnvironment.loader(fixture).getHarness(
          LgChronicleHarness,
        );
    });

    afterEach(() => fixture.destroy());

    it("makes each author's name a button", async () => {
      expect(await chronicle.getAuthorButtons()).toEqual([
        'Maren',
        'From Kaelen',
      ]);
    });

    it("a press on an author's name emits authorSelect once, with the message and the pressed name", async () => {
      await chronicle.pressAuthor('Maren');

      const selected = fixture.componentInstance.selected;
      expect(selected.length).toBe(1);
      expect(selected[0].message).toBe(MESSAGES_NOBLE[0]);
      expect(selected[0].element.textContent?.trim()).toBe('Maren');
      expect(await chronicle.isAuthorFocused('Maren')).toBeTrue();
      expect(document.activeElement).toBe(selected[0].element);
    });
  });

  describe('channels and collapse (i-chronicle)', () => {
    let fixture: ComponentFixture<ChannelsHost>;
    let chronicle: LgChronicleHarness;

    beforeEach(async () => {
      fixture = TestBed.createComponent(ChannelsHost);
      chronicle =
        await TestbedHarnessEnvironment.loader(fixture).getHarness(
          LgChronicleHarness,
        );
    });

    afterEach(() => fixture.destroy());

    it('starts open on the bound channel, with one channel tab in the Tab order', async () => {
      expect(await chronicle.getLabel()).toBe('Chronicle');
      expect(await chronicle.isOpen()).toBeTrue();
      expect(await chronicle.getChannels()).toEqual([
        'All',
        'General',
        'Trade',
      ]);
      expect(await chronicle.getSelectedChannel()).toBe('All');
      expect(await chronicle.getChannelTabStops()).toEqual(['All']);
      expect((await chronicle.getLines()).length).toBe(4);
    });

    it('a press on a channel selects it, shows only its lines and moves the tab stop to it', async () => {
      await chronicle.pressChannel('General');

      expect(await chronicle.getSelectedChannel()).toBe('General');
      expect(await chronicle.getChannelTabStops()).toEqual(['General']);
      expect(await chronicle.getFocusedChannel()).toBe('General');
      expect(fixture.componentInstance.channel()).toBe('general');
      const lines = await chronicle.getLines();
      expect(lines.length).toBe(1);
      expect(lines[0]).toContain('Anyone for the tower?');
    });

    it('ArrowRight on the channels selects and focuses the next one', async () => {
      await chronicle.pressChannel('General');
      await chronicle.pressChannelKey(TestKey.RIGHT_ARROW);

      expect(await chronicle.getSelectedChannel()).toBe('Trade');
      expect(await chronicle.getFocusedChannel()).toBe('Trade');
      expect(await chronicle.getChannelTabStops()).toEqual(['Trade']);
      expect(fixture.componentInstance.channel()).toBe('trade');
    });

    it('ArrowRight on the last channel wraps to the first, and ArrowLeft wraps back', async () => {
      await chronicle.pressChannel('Trade');
      await chronicle.pressChannelKey(TestKey.RIGHT_ARROW);
      expect(await chronicle.getSelectedChannel()).toBe('All');
      expect(await chronicle.getFocusedChannel()).toBe('All');

      await chronicle.pressChannelKey(TestKey.LEFT_ARROW);
      expect(await chronicle.getSelectedChannel()).toBe('Trade');
      expect(await chronicle.getFocusedChannel()).toBe('Trade');
    });

    it('the collapse toggle collapses it to the ticker, which names the unread count', async () => {
      expect(await chronicle.getCollapseToggleLabel()).toBe('Collapse chat');
      expect(await chronicle.isCollapseToggleExpanded()).toBeTrue();

      await chronicle.pressChannel('General');
      await chronicle.pressChannelKey(TestKey.RIGHT_ARROW);
      await chronicle.pressCollapseToggle();

      expect(await chronicle.isOpen()).toBeFalse();
      expect(fixture.componentInstance.open()).toBeFalse();
      expect(await chronicle.isCollapseToggleExpanded()).toBeFalse();
      expect(await chronicle.getCollapseToggleLabel()).toBe('Expand chat');
      expect(await chronicle.isCollapseToggleFocused()).toBeTrue();
      expect(await chronicle.getTickerLabel()).toBe('Open chat, 2 unread');
      expect(await chronicle.getSelectedChannel()).toBeNull();
    });

    it('a press on the ticker opens it again on the channel the player left', async () => {
      await chronicle.pressChannel('General');
      await chronicle.pressChannelKey(TestKey.RIGHT_ARROW);
      await chronicle.pressCollapseToggle();
      await chronicle.pressTicker();

      expect(await chronicle.isOpen()).toBeTrue();
      expect(fixture.componentInstance.open()).toBeTrue();
      expect(await chronicle.isCollapseToggleExpanded()).toBeTrue();
      expect(await chronicle.getTickerLabel()).toBeNull();
      expect(await chronicle.getSelectedChannel()).toBe('Trade');
      expect(fixture.componentInstance.channel()).toBe('trade');
    });
  });

  describe('live lines (i-chronicle-live)', () => {
    let fixture: ComponentFixture<LiveHost>;
    let chronicle: LgChronicleHarness;

    // Real waits here, not fakeAsync: where the log sits and the scroll events it sends come from the browser's
    // layout and frames. Change detection runs as in the app, so the count the log sets after it renders shows.
    beforeEach(async () => {
      fixture = TestBed.createComponent(LiveHost);
      fixture.autoDetectChanges();
      chronicle =
        await TestbedHarnessEnvironment.loader(fixture).getHarness(
          LgChronicleHarness,
        );
      await nextFrame();
    });

    afterEach(() => fixture.destroy());

    async function addLines(n: number): Promise<void> {
      for (let i = 0; i < n; i++) fixture.componentInstance.addLine();
      await fixture.whenStable();
      await nextFrame();
    }

    it('opens at the foot of the log, the newest line in view', async () => {
      expect(await chronicle.canLogScroll()).toBeTrue();
      expect(await chronicle.isLogAtFoot()).toBeTrue();
      expect(await chronicle.getNewLinesLabel()).toBeNull();
    });

    it('follows new lines while the player is at the foot', async () => {
      await addLines(2);

      expect(await chronicle.isLogAtFoot()).toBeTrue();
      expect(await chronicle.getNewLinesLabel()).toBeNull();
    });

    it('scrolled up, keeps the reader’s place as lines arrive and counts them', async () => {
      await chronicle.scrollLogToTop();
      await addLines(2);

      expect(await chronicle.getLogScrollTop()).toBe(0);
      expect(await chronicle.isLogAtFoot()).toBeFalse();
      expect(await chronicle.getNewLinesLabel()).toBe('2 new lines');
      expect(await chronicle.getNewLinesName()).toBe(
        '2 new lines, jump to latest',
      );
    });

    it('"N new lines" jumps to the latest, focuses the log and goes away', async () => {
      await chronicle.scrollLogToTop();
      await addLines(2);
      await chronicle.jumpToLatest();
      await nextFrame();

      expect(await chronicle.isLogAtFoot()).toBeTrue();
      expect(await chronicle.isLogFocused()).toBeTrue();
      expect(await chronicle.getNewLinesLabel()).toBeNull();
    });

    it('new lines are read by the log itself, a polite live region, not through the announcer', async () => {
      const watch = lgWatchAnnouncements();
      try {
        await addLines(2);
        // The announcer writes 60 ms after it is asked.
        await new Promise((resolve) => setTimeout(resolve, 100));

        expect(await chronicle.getLogLiveMode()).toBe('polite');
        expect(watch.said).toEqual([]);
      } finally {
        watch.stop();
      }
    });
  });
});
