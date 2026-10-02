import { Component } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgCurrencyPillComponent } from './currency-pill.component';
import { LgCurrencyPillHarness } from '../../testing/currency-pill.harness';
import { lgCloseTip } from '../../testing/tip.harness';

/** The parity case i-currency: a short pill, which toggles its own format; and a pill whose press is the screen's. */
@Component({
  imports: [LgCurrencyPillComponent],
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <button
      lgCurrencyPill
      name="Cinders"
      [amount]="12480"
      short
      toggle
    ></button>
    <button
      lgCurrencyPill
      name="Soulstones"
      [amount]="36400"
      [short]="short"
      tooltip="Toggle abbreviated currency values"
      (click)="short = !short"
    ></button>
  `,
})
class ShortPillCase {
  short = true;
}

describe('LgCurrencyPillComponent', () => {
  afterEach(lgCloseTip);

  describe('short (i-currency)', () => {
    let pill: LgCurrencyPillHarness;

    beforeEach(async () => {
      const fixture = TestBed.createComponent(ShortPillCase);
      pill = await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgCurrencyPillHarness.with({ name: 'Cinders' }),
      );
    });

    it('shows the full figure on a press, and the short one again on a second press', async () => {
      expect(await pill.getShownAmount()).toBe('12.5k');
      await pill.press();
      expect(await pill.getShownAmount()).toBe('12,480');
      await pill.press();
      expect(await pill.getShownAmount()).toBe('12.5k');
    });

    it('gives screen readers and its tip the full amount, whatever it shows', fakeAsync(async () => {
      expect(await pill.getAccessibleName()).toBe('12,480 Cinders');
      expect(await pill.getTooltip()).toBe('12,480 Cinders');
      await pill.press();
      expect(await pill.getAccessibleName()).toBe('12,480 Cinders');
      expect(await pill.getTooltip()).toBe('12,480 Cinders');
      flush();
    }));

    it('keeps the width of the widest figure it has shown, so it does not shrink back', async () => {
      expect(await pill.getReservedWidth()).toBe(5);
      await pill.press();
      expect(await pill.getReservedWidth()).toBe(6);
      await pill.press();
      expect(await pill.getReservedWidth()).toBe(6);
    });
  });

  describe('a press of the screen’s own', () => {
    it('keeps its format itself; the screen’s (click) switches it, and the tip says what a press does', fakeAsync(async () => {
      const fixture = TestBed.createComponent(ShortPillCase);
      const pill = await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgCurrencyPillHarness.with({ name: 'Soulstones' }),
      );

      expect(await pill.getShownAmount()).toBe('36.4k');
      await pill.press();
      expect(await pill.getShownAmount()).toBe('36,400');
      expect(await pill.getTooltip()).toBe(
        'Toggle abbreviated currency values',
      );
      expect(await pill.getAccessibleName()).toBe('36,400 Soulstones');
      flush();
    }));
  });
});
