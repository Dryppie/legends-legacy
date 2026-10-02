import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { ComponentHarness } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgRarity } from '../../core/grimoire-core';
import { LgRarityComponent } from './rarity.component';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';

class LgRarityHarness extends ComponentHarness {
  static hostSelector = 'lg-rarity';
}

@Component({
  imports: [LgRarityComponent],
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `<lg-rarity [rarity]="rarity()" nameId="spoken" />`,
})
class RarityHost {
  readonly rarity = signal<LgRarity>('Epic');
}

describe('LgRarityComponent', () => {
  afterEach(lgCloseTip);

  it('shows the code in the rarity’s hue and says the name', () => {
    const fixture = TestBed.createComponent(RarityHost);
    fixture.detectChanges();
    const mark = fixture.nativeElement.querySelector(
      'lg-rarity',
    ) as HTMLElement;

    expect(mark.querySelector('.lg-rarity__code')?.textContent).toBe('E');
    expect(
      mark.querySelector('.lg-rarity__code')?.getAttribute('aria-hidden'),
    ).toBe('true');
    expect(document.getElementById('spoken')?.textContent?.trim()).toBe(
      ', Epic',
    );
    expect(mark.classList).toContain('lg-rarity--epic');

    fixture.componentInstance.rarity.set('Legacy');
    fixture.detectChanges();
    expect(mark.querySelector('.lg-rarity__code')?.textContent).toBe('LG');
    expect(getComputedStyle(mark).color).not.toBe('');
  });

  it('names the rarity in the tip on hover, without a description', fakeAsync(async () => {
    const fixture = TestBed.createComponent(RarityHost);
    const mark =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgRarityHarness,
      );
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);

    await (await mark.host()).hover();
    const tip = await page.getHarnessOrNull(LgTipHarness.with({ shown: true }));

    expect(await tip?.getReason()).toBe('Epic');
    expect(
      await (await mark.host()).getAttribute('aria-describedby'),
    ).toBeNull();
    flush();
  }));
});
