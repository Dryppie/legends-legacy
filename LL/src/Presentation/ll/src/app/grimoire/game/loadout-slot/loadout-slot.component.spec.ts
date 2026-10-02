import { Component } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  tick,
} from '@angular/core/testing';
import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgLoadoutSlotComponent } from './loadout-slot.component';
import { LgLoadoutSlotHarness } from '../../testing/loadout-slot.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

/** The parity case i-loadout-locked. */
@Component({
  imports: [LgLoadoutSlotComponent],
  // Kept in view, like the other reason-tip cases, so "no tip" means none was asked for.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <lg-loadout-slot
      [index]="2"
      state="locked"
      reason="Unlocks at level 20"
      interactive
      (activate)="activated = activated + 1"
    />
  `,
})
class LockedLoadoutCase {
  activated = 0;
}

describe('LgLoadoutSlotComponent', () => {
  let fixture: ComponentFixture<LockedLoadoutCase>;
  let slot: () => Promise<LgLoadoutSlotHarness>;
  let page: HarnessLoader;

  beforeEach(lgAnnouncerIdle);

  beforeEach(() => {
    fixture = TestBed.createComponent(LockedLoadoutCase);
    slot = () =>
      TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgLoadoutSlotHarness,
      );
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => lgCloseTip());

  describe('locked (i-loadout-locked)', () => {
    it('announces its condition on a press, opens no tip, and does not emit activate', fakeAsync(async () => {
      lgQuietAnnouncer();
      const announcements = lgWatchAnnouncements();

      await (await slot()).press();
      tick(250);

      expect(
        await page.getHarnessOrNull(LgTipHarness.with({ shown: true })),
      ).toBeNull();
      expect(announcements.said).toEqual([
        'polite: Locked. Unlocks at level 20',
      ]);
      expect(fixture.componentInstance.activated).toBe(0);
      announcements.stop();
    }));

    it('stays a button, blocked, with its printed condition in its name and no separate description', async () => {
      const s = await slot();

      expect(await s.isBlocked()).toBeTrue();
      expect(await s.getTag()).toBe('Locked');
      expect(await s.getName()).toBe('Unlocks at level 20');
      const name = await s.getAccessibleName();
      expect(name).toMatch(/^Slot 3/);
      expect(name).toContain('Locked');
      expect(name).toMatch(/Unlocks at level 20$/);
      expect(await s.getDescription()).toBeNull();
    });
  });
});
