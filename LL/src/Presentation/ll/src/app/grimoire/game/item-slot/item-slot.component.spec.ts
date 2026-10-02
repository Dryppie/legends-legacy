import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgItemSlotComponent } from './item-slot.component';
import { LgItemSlotHarness } from '../../testing/item-slot.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

/** The parity case i-itemslot-locked: a captioned slot, its reason printed under its name. */
@Component({
  imports: [LgItemSlotComponent],
  // Kept in view: the reason tip closes when its control is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <lg-item-slot
      name="Tower Key"
      state="locked"
      reason="Clear floor 10"
      interactive
      (activate)="activated = activated + 1"
    />
  `,
})
class LockedSlotCase {
  activated = 0;
}

/** The parity case i-itemslot-nocaption: no caption, so the reason opens in the reason tip. */
@Component({
  imports: [LgItemSlotComponent],
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <lg-item-slot
      icon="essences"
      [caption]="false"
      state="cooldown"
      [remaining]="3725"
      interactive
      (activate)="activated = activated + 1"
    />
  `,
})
class UncaptionedSlotCase {
  activated = 0;
}

describe('LgItemSlotComponent', () => {
  beforeEach(lgAnnouncerIdle);

  afterEach(() => lgCloseTip());

  describe('locked, with a caption (i-itemslot-locked)', () => {
    let fixture: ComponentFixture<LockedSlotCase>;
    let slot: () => Promise<LgItemSlotHarness>;
    let page: HarnessLoader;

    beforeEach(() => {
      fixture = TestBed.createComponent(LockedSlotCase);
      slot = () =>
        TestbedHarnessEnvironment.loader(fixture).getHarness(
          LgItemSlotHarness.with({ name: 'Tower Key' }),
        );
      page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    });

    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));

    it('announces its printed reason on a press, opens no tip, and does not emit activate', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();

      await (await slot()).press();
      tick(250);

      expect(await shownTip()).toBeNull();
      expect(announcements.said).toEqual(['polite: Locked. Clear floor 10']);
      expect(fixture.componentInstance.activated).toBe(0);
      announcements.stop();
    }));

    it('opens no tip on hover, after a press or not: the reason is already printed', fakeAsync(async () => {
      const s = await slot();

      await s.hover();
      expect(await shownTip()).toBeNull();
      await s.press();
      tick(250);
      await s.hover();
      expect(await shownTip()).toBeNull();
    }));

    it('stays a focusable button, blocked, with its printed reason as its description', async () => {
      const s = await slot();

      expect(await s.isBlocked()).toBeTrue();
      expect(await s.isPressed()).toBeNull();
      expect(await s.getPrintedWord()).toBe('Locked');
      expect(await s.getPrintedReason()).toBe('Clear floor 10');
      expect(await s.getDescription()).toBe('Locked. Clear floor 10');
      await s.focus();
      expect(await s.isFocused()).toBeTrue();
    });
  });

  describe('blocked, without a caption (i-itemslot-nocaption)', () => {
    let fixture: ComponentFixture<UncaptionedSlotCase>;
    let slot: () => Promise<LgItemSlotHarness>;
    let page: HarnessLoader;

    beforeEach(() => {
      fixture = TestBed.createComponent(UncaptionedSlotCase);
      slot = () =>
        TestbedHarnessEnvironment.loader(fixture).getHarness(LgItemSlotHarness);
      page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    });

    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));

    it('shows its reason in the tip, unpinned, on hover, and says nothing', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const s = await slot();

      await s.hover();

      const tip = await shownTip();
      expect(tip).not.toBeNull();
      expect(await tip!.getWord()).toBeNull();
      expect(await tip!.getReason()).toBe('Ready in 1h 2m');
      expect(await tip!.isPinned(s)).toBeFalse();
      expect(announcements.said).toEqual([]);
      announcements.stop();
    }));

    it('pins the tip and announces the spoken reason on a press, and does not emit activate', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const s = await slot();

      await s.hover();
      await s.press();
      tick(250);

      const tip = await shownTip();
      expect(tip).not.toBeNull();
      expect(await tip!.getReason()).toBe('Ready in 1h 2m');
      expect(await tip!.isPinned(s)).toBeTrue();
      expect(announcements.said).toEqual(['polite: Ready in 1 hour 2 minutes']);
      expect(fixture.componentInstance.activated).toBe(0);
      announcements.stop();
    }));

    it('is blocked, prints nothing, and is described by the spoken reason', async () => {
      const s = await slot();

      expect(await s.isBlocked()).toBeTrue();
      expect(await s.getPrintedReason()).toBeNull();
      expect(await s.getDescription()).toBe('Ready in 1 hour 2 minutes');
    });
  });
});
