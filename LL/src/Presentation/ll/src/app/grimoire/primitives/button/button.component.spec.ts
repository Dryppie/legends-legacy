import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgButtonComponent } from './button.component';
import { LgButtonHarness } from '../../testing/button.harness';
import {
  LgReasonTipHarness,
  lgCloseReasonTip,
} from '../../testing/reason-tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

type ButtonCase = 'locked' | 'ok' | 'pending';

/** The parity cases i-button-locked, i-button-ok and i-button-pending. */
@Component({
  imports: [LgButtonComponent],
  // Kept in view: the reason tip closes when its control is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <button
      lgButton
      state="locked"
      reason="Unlocks at level 20"
      (click)="count('locked')"
    >
      Ascend
    </button>
    <button lgButton (click)="count('ok')">Go</button>
    <button lgButton state="pending" (click)="count('pending')">Claim</button>
  `,
})
class ButtonCases {
  readonly clicks: Record<ButtonCase, number> = {
    locked: 0,
    ok: 0,
    pending: 0,
  };
  count(which: ButtonCase): void {
    this.clicks[which]++;
  }
}

describe('LgButtonComponent', () => {
  let fixture: ComponentFixture<ButtonCases>;
  let loader: HarnessLoader;
  let page: HarnessLoader;

  beforeEach(lgAnnouncerIdle);

  beforeEach(() => {
    fixture = TestBed.createComponent(ButtonCases);
    loader = TestbedHarnessEnvironment.loader(fixture);
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => lgCloseReasonTip());

  const clicks = () => fixture.componentInstance.clicks;
  const shownTip = () =>
    page.getHarnessOrNull(LgReasonTipHarness.with({ shown: true }));

  describe('locked (i-button-locked)', () => {
    const locked = () =>
      loader.getHarness(LgButtonHarness.with({ label: 'Ascend' }));

    it('shows its reason tip, unpinned, while the pointer is over it, and says nothing', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const button = await locked();

      await button.hover();

      const tip = await shownTip();
      expect(tip).not.toBeNull();
      expect(await tip!.getWord()).toBe('Locked');
      expect(await tip!.getReason()).toBe('Unlocks at level 20');
      expect(await tip!.isPinned(button)).toBeFalse();
      expect(announcements.said).toEqual([]);
      expect(clicks().locked).toBe(0);
      announcements.stop();
    }));

    it('pins its reason tip and announces the reason on a press, and does not run (click)', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const button = await locked();

      await button.hover();
      await button.press();
      tick(250);

      const tip = await shownTip();
      expect(tip).not.toBeNull();
      expect(await tip!.getWord()).toBe('Locked');
      expect(await tip!.getReason()).toBe('Unlocks at level 20');
      expect(await tip!.isPinned(button)).toBeTrue();
      expect(announcements.said).toEqual([
        'polite: Locked. Unlocks at level 20',
      ]);
      expect(clicks().locked).toBe(0);
      announcements.stop();
    }));

    it('closes its pinned tip on a second press, without saying it again or running (click)', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const button = await locked();

      await button.hover();
      await button.press();
      tick(250);
      await button.press();

      expect(await shownTip()).toBeNull();
      expect(announcements.said).toEqual([
        'polite: Locked. Unlocks at level 20',
      ]);
      expect(clicks().locked).toBe(0);
      announcements.stop();
    }));

    it('stays focusable, blocked rather than disabled, and is described by its reason', async () => {
      const button = await locked();

      expect(await button.isBlocked()).toBeTrue();
      expect(await button.isDisabled()).toBeFalse();
      expect(await button.getDescription()).toBe('Locked. Unlocks at level 20');
      await button.focus();
      expect(await button.isFocused()).toBeTrue();
      await button.blur();
    });
  });

  it('runs (click) once on a press when available, with no reason tip and nothing said (i-button-ok)', fakeAsync(async () => {
    lgQuietAnnouncer();
    const announcements = lgWatchAnnouncements();
    const button = await loader.getHarness(
      LgButtonHarness.with({ label: 'Go' }),
    );

    await button.press();

    expect(clicks().ok).toBe(1);
    expect(await button.isBlocked()).toBeFalse();
    expect(await button.isPending()).toBeFalse();
    expect(await shownTip()).toBeNull();
    expect(announcements.said).toEqual([]);
    announcements.stop();
  }));

  it('ignores a press while pending: busy, showing the pending word, (click) not run (i-button-pending)', fakeAsync(async () => {
    lgQuietAnnouncer();
    const announcements = lgWatchAnnouncements();
    const button = await loader.getHarness(
      LgButtonHarness.with({ label: 'Saving…' }),
    );

    await button.press();

    expect(clicks().pending).toBe(0);
    expect(await button.isPending()).toBeTrue();
    expect(await button.isBlocked()).toBeFalse();
    expect(await button.getLabel()).toBe('Saving…');
    expect(await shownTip()).toBeNull();
    expect(announcements.said).toEqual([]);
    announcements.stop();
  }));
});
