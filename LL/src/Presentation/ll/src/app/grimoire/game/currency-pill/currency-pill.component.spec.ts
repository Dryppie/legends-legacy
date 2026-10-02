import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgCurrencyPillComponent } from './currency-pill.component';
import { LgCurrencyPillHarness } from '../../testing/currency-pill.harness';

/** The parity case i-currency: a short pill, which toggles its own format. */
@Component({
  imports: [LgCurrencyPillComponent],
  template: `<lg-currency-pill name="Cinders" [amount]="12480" short />`,
})
class ShortPillCase {}

describe('LgCurrencyPillComponent', () => {
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

    it('gives screen readers and its tooltip the full amount, whatever it shows', async () => {
      expect(await pill.getAccessibleName()).toBe('12,480 Cinders');
      expect(await pill.getTooltip()).toBe('12,480 Cinders');
      await pill.press();
      expect(await pill.getAccessibleName()).toBe('12,480 Cinders');
      expect(await pill.getTooltip()).toBe('12,480 Cinders');
    });

    it('keeps the width of the widest figure it has shown, so it does not shrink back', async () => {
      expect(await pill.getReservedWidth()).toBe(5);
      await pill.press();
      expect(await pill.getReservedWidth()).toBe(6);
      await pill.press();
      expect(await pill.getReservedWidth()).toBe(6);
    });
  });
});
