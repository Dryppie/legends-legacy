import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HarnessLoader, TestKey } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgChronicleHarness } from '../../testing/chronicle.harness';
import { LgGameShellHarness } from '../../testing/game-shell.harness';
import { LgTopBarHarness } from '../../testing/top-bar.harness';
import {
  LgChronicleChannel,
  LgChronicleComponent,
  LgChronicleMessage,
} from '../chronicle/chronicle.component';
import { LgGameShellComponent } from './game-shell.component';
import { LgChroniclePosition, LgSlotDirective } from '../../core/grimoire-core';
import { Overlay } from '@angular/cdk/overlay';
import { DomPortal } from '@angular/cdk/portal';
import {
  LgNavRailComponent,
  LgNavSection,
} from '../nav-rail/nav-rail.component';
import { LgTopBarComponent } from '../top-bar/top-bar.component';

// The data the parity cases used (D-130), before the parity check was retired.
const NAV: LgNavSection[] = [
  {
    label: 'Character',
    items: [
      { id: 'overview', title: 'Overview', icon: 'overview' },
      {
        id: 'inventory',
        title: 'Inventory',
        icon: 'inventory',
        badge: 3,
        badgeLabel: '3 new items',
      },
      {
        id: 'essences',
        title: 'Essences',
        icon: 'essences',
        locked: true,
        reason: 'Unlocks at level 20',
      },
    ],
  },
  {
    label: 'City',
    items: [{ id: 'guild', title: 'Guild', icon: 'guild', href: '/guild' }],
  },
];
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

// The parity case i-shell-narrow: an 800px-wide shell, under 60rem, so the rail is a drawer the TopBar's menu opens.
@Component({
  imports: [
    LgGameShellComponent,
    LgNavRailComponent,
    LgTopBarComponent,
    LgSlotDirective,
  ],
  template: `
    <div style="width: 800px">
      <lg-game-shell [height]="500">
        <lg-nav-rail lgSlot="rail" [sections]="nav" activeId="overview" />
        <lg-top-bar lgSlot="top" title="Aldric" showMenu />
        <p>Stage</p>
      </lg-game-shell>
    </div>
  `,
})
class NarrowShellHost {
  readonly nav = NAV;
}

// The parity case i-shell-floating: a 1200px-wide shell with the floating Chronicle drawer. Its position is bound
// two-way here, as a game persists it.
@Component({
  imports: [LgGameShellComponent, LgChronicleComponent, LgSlotDirective],
  template: `
    <div style="width: 1200px">
      <lg-game-shell
        [height]="500"
        chatLayout="floating"
        [(chroniclePosition)]="position"
      >
        <lg-chronicle
          lgSlot="chronicle"
          [channels]="channels"
          [messages]="messages"
          [composer]="false"
        />
        <p>Stage</p>
      </lg-game-shell>
    </div>
  `,
})
class FloatingShellHost {
  readonly channels = CHANNELS;
  readonly messages = MESSAGES;
  readonly position = signal<LgChroniclePosition | null>(null);
}

describe('LgGameShellComponent', () => {
  describe('the rail drawer under 60rem (i-shell-narrow)', () => {
    let fixture: ComponentFixture<NarrowShellHost>;
    let shell: LgGameShellHarness;
    let topBar: LgTopBarHarness;

    beforeEach(async () => {
      fixture = TestBed.createComponent(NarrowShellHost);
      const loader: HarnessLoader = TestbedHarnessEnvironment.loader(fixture);
      shell = await loader.getHarness(LgGameShellHarness);
      topBar = await loader.getHarness(LgTopBarHarness);
    });

    afterEach(() => fixture.destroy());

    it('starts closed, with the menu button in the TopBar', async () => {
      expect(await topBar.hasMenu()).toBeTrue();
      expect(await shell.isRailOpen()).toBeFalse();
      expect(await shell.isStageInert()).toBeFalse();
    });

    it('the menu opens the drawer, moves focus to its first item and makes the rest inert', async () => {
      await topBar.pressMenu();

      expect(await shell.isRailOpen()).toBeTrue();
      expect(await shell.isStageInert()).toBeTrue();
      expect(await shell.getFocusedRailItem()).toBe('Overview');
      expect(await topBar.isMenuFocused()).toBeFalse();
    });

    it('Escape closes the drawer and returns focus to the menu button', async () => {
      await topBar.pressMenu();
      await shell.pressKey(TestKey.ESCAPE);

      expect(await shell.isRailOpen()).toBeFalse();
      expect(await shell.isStageInert()).toBeFalse();
      expect(await shell.getFocusedRailItem()).toBeNull();
      expect(await topBar.isMenuFocused()).toBeTrue();
    });

    // Fixed in plan step 11 (D-134): an overlay marks the Escape it uses, and the drawer leaves a marked Escape alone.
    it('an Escape that closes an overlay opened from the drawer leaves the drawer open', async () => {
      await topBar.pressMenu();
      // A popover on the CDK overlay, as an lg-popover will be: it closes on Escape and marks the key as used.
      const content = document.createElement('div');
      content.textContent = 'Popover';
      document.body.appendChild(content);
      const popover = TestBed.inject(Overlay).create();
      popover.attach(new DomPortal(content));
      popover.keydownEvents().subscribe((e) => {
        if (e.key !== 'Escape') return;
        e.preventDefault();
        popover.detach();
      });
      try {
        await shell.pressKey(TestKey.ESCAPE);

        expect(popover.hasAttached()).toBeFalse();
        expect(await shell.isRailOpen()).toBeTrue();
        expect(await shell.getFocusedRailItem()).toBe('Overview');

        await shell.pressKey(TestKey.ESCAPE);
        expect(await shell.isRailOpen()).toBeFalse();
      } finally {
        popover.dispose();
        content.remove();
      }
    });
  });

  describe('the floating Chronicle drawer (i-shell-floating)', () => {
    let fixture: ComponentFixture<FloatingShellHost>;
    let shell: LgGameShellHarness;
    let chronicle: LgChronicleHarness;

    beforeEach(async () => {
      fixture = TestBed.createComponent(FloatingShellHost);
      const loader = TestbedHarnessEnvironment.loader(fixture);
      shell = await loader.getHarness(LgGameShellHarness);
      chronicle = await loader.getHarness(LgChronicleHarness);
    });

    afterEach(() => fixture.destroy());

    it('gives the Chronicle a drag grip and a tall toggle', async () => {
      expect(await chronicle.hasGrip()).toBeTrue();
      expect(await chronicle.hasTallToggle()).toBeTrue();
      expect(await chronicle.isTall()).toBeFalse();
      expect(await chronicle.getTallToggleLabel()).toBe('Make chat taller');
      expect(fixture.componentInstance.position()).toBeNull();
    });

    it('a press on the grip alone does not move the drawer', async () => {
      const start = await shell.getChroniclePosition();
      await chronicle.pressGrip();

      expect(await chronicle.isGripFocused()).toBeTrue();
      expect(await shell.getChroniclePosition()).toEqual(start);
      expect(fixture.componentInstance.position()).toBeNull();
    });

    it('the arrow keys on the grip nudge the drawer 16px and report its new position', async () => {
      const start = await shell.getChroniclePosition();
      await chronicle.pressGrip();
      await chronicle.pressGripKey(TestKey.RIGHT_ARROW);
      await chronicle.pressGripKey(TestKey.UP_ARROW);

      const moved = { left: start.left + 16, bottom: start.bottom + 16 };
      expect(await shell.getChroniclePosition()).toEqual(moved);
      expect(fixture.componentInstance.position()).toEqual(moved);
      expect(await chronicle.isGripFocused()).toBeTrue();
    });

    it('the shell keeps the nudged drawer inside its frame', async () => {
      await chronicle.pressGrip();
      for (let i = 0; i < 4; i++)
        await chronicle.pressGripKey(TestKey.RIGHT_ARROW);
      const atEdge = await shell.getChroniclePosition();
      await chronicle.pressGripKey(TestKey.RIGHT_ARROW);

      // 8px from the shell's right edge: 1200 wide, less the drawer's width.
      expect(await shell.getChroniclePosition()).toEqual(atEdge);
      expect(fixture.componentInstance.position()!.left).toBe(atEdge.left);
    });

    // Not its height: the drawer is never taller than the screen, and the test window is shorter than either height.
    it('the tall toggle makes the drawer taller and says so, and makes it shorter again', async () => {
      await chronicle.pressGrip();
      await chronicle.pressGripKey(TestKey.RIGHT_ARROW);
      await chronicle.pressGripKey(TestKey.UP_ARROW);
      const position = await shell.getChroniclePosition();
      await chronicle.pressTallToggle();

      expect(await chronicle.isTall()).toBeTrue();
      expect(await chronicle.getTallToggleLabel()).toBe('Make chat shorter');
      expect(await chronicle.isTallToggleFocused()).toBeTrue();
      expect(await shell.getChroniclePosition()).toEqual(position);

      await chronicle.pressTallToggle();
      expect(await chronicle.isTall()).toBeFalse();
      expect(await chronicle.getTallToggleLabel()).toBe('Make chat taller');
    });
  });
});
