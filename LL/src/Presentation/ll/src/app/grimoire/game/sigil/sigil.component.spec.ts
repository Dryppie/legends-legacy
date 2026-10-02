import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgSigilComponent } from './sigil.component';
import { LgSigilHarness } from '../../testing/sigil.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

/** The parity case i-sigil-locked. */
@Component({
  imports: [LgSigilComponent],
  // Kept in view: the reason tip closes when its control is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <lg-sigil
      [value]="9"
      label="Int"
      state="locked"
      reason="Unlocks at level 30"
      interactive
      (activate)="activated = activated + 1"
    />
  `,
})
class LockedSigilCase {
  activated = 0;
}

describe('LgSigilComponent', () => {
  let fixture: ComponentFixture<LockedSigilCase>;
  let sigil: () => Promise<LgSigilHarness>;
  let page: HarnessLoader;

  beforeEach(lgAnnouncerIdle);

  beforeEach(() => {
    fixture = TestBed.createComponent(LockedSigilCase);
    sigil = () =>
      TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgSigilHarness.with({ label: 'Int' }),
      );
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => lgCloseTip());

  describe('locked (i-sigil-locked)', () => {
    it('pins its reason tip and announces its condition on a press, and does not emit activate', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();
      const s = await sigil();

      await s.press();
      tick(250);

      const tip = await page.getHarnessOrNull(
        LgTipHarness.with({ shown: true }),
      );
      expect(tip).not.toBeNull();
      expect(await tip!.getWord()).toBe('Locked');
      expect(await tip!.getReason()).toBe('Unlocks at level 30');
      expect(await tip!.isPinned(s)).toBeTrue();
      expect(announcements.said).toEqual([
        'polite: Locked. Unlocks at level 30',
      ]);
      expect(fixture.componentInstance.activated).toBe(0);
      announcements.stop();
    }));

    it('stays a focusable button, blocked rather than a toggle, named by its stat and described by its condition', async () => {
      const s = await sigil();

      expect(await s.isBlocked()).toBeTrue();
      expect(await s.isPressed()).toBeNull();
      expect(await s.getAccessibleName()).toBe('Int 9');
      expect(await s.getDescription()).toBe('Locked. Unlocks at level 30');
      await s.focus();
      expect(await s.isFocused()).toBeTrue();
    });
  });
});
