import { Component, signal } from '@angular/core';
import {
  ComponentFixture,
  TestBed,
  fakeAsync,
  flush,
} from '@angular/core/testing';
import {
  ComponentHarness,
  HarnessLoader,
  HarnessPredicate,
} from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgTooltipDirective } from './tooltip.directive';
import {
  LgTipHarness,
  lgCloseTip,
  lgDescriptionOf,
} from '../../testing/tip.harness';

@Component({
  imports: [LgTooltipDirective],
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0' },
  template: `
    <button type="button" class="plain" [lgTooltip]="text()">Rare</button>
    <button
      type="button"
      class="explained"
      lgTooltipPin
      [lgTooltip]="{
        title: 'Power',
        text: 'Raises every damage roll.',
        meta: 'From Strength',
        kind: 'explanation',
      }"
    >
      Power
    </button>
    <button
      type="button"
      class="templated"
      [lgTooltip]="tpl"
      lgTooltipDescription="Hit chance and crit chance"
    >
      Precision
    </button>
    <ng-template #tpl><b class="from-template">Hit</b> and crit</ng-template>
    <button type="button" class="away">Away</button>
  `,
})
class TooltipCases {
  readonly text = signal<string | null>('Rare: the third of seven rarities');
}

/** One of the case's buttons, by its class. */
class TargetHarness extends ComponentHarness {
  static hostSelector = 'button';
  static withClass(cls: string): HarnessPredicate<TargetHarness> {
    return new HarnessPredicate(TargetHarness, { selector: '.' + cls });
  }
  async hover(): Promise<void> {
    return (await this.host()).hover();
  }
  async mouseAway(): Promise<void> {
    return (await this.host()).mouseAway();
  }
  async focus(): Promise<void> {
    return (await this.host()).focus();
  }
  async blur(): Promise<void> {
    return (await this.host()).blur();
  }
  async click(): Promise<void> {
    return (await this.host()).click();
  }
  /** What screen readers hear as its description. */
  async getDescription(): Promise<string | null> {
    return lgDescriptionOf(
      await this.host(),
      this.documentRootLocatorFactory(),
    );
  }
}

describe('LgTooltipDirective', () => {
  let fixture: ComponentFixture<TooltipCases>;
  let loader: HarnessLoader;
  let page: HarnessLoader;

  beforeEach(() => {
    fixture = TestBed.createComponent(TooltipCases);
    fixture.detectChanges();
    loader = TestbedHarnessEnvironment.loader(fixture);
    page = TestbedHarnessEnvironment.documentRootLoader(fixture);
  });

  afterEach(() => lgCloseTip());

  const button = (cls: string) =>
    loader.getHarness(TargetHarness.withClass(cls));
  const shown = () => page.getHarnessOrNull(LgTipHarness.with({ shown: true }));

  it('shows its words beside the element on hover, and closes a moment after the pointer leaves', fakeAsync(async () => {
    const plain = await button('plain');

    await plain.hover();
    const tip = await shown();
    expect(tip).not.toBeNull();
    expect(await tip!.getReason()).toBe('Rare: the third of seven rarities');

    await plain.mouseAway();
    flush();
    expect(await shown()).toBeNull();
  }));

  it('is the description of its element, so screen readers hear it', async () => {
    const plain = await button('plain');

    expect(await plain.getDescription()).toBe(
      'Rare: the third of seven rarities',
    );
  });

  it('follows its words: new words describe the element, and none removes the description', async () => {
    const plain = await button('plain');
    fixture.componentInstance.text.set('Rare');
    fixture.detectChanges();
    expect(await plain.getDescription()).toBe('Rare');

    fixture.componentInstance.text.set(null);
    fixture.detectChanges();
    expect(await plain.getDescription()).toBeNull();
    await plain.hover();
    expect(await shown()).toBeNull();
  });

  it('shows on keyboard focus and closes when focus moves on', async () => {
    const plain = await button('plain');

    await plain.focus();
    expect(await shown()).not.toBeNull();
    await plain.blur();
    expect(await shown()).toBeNull();
  });

  it('closes on Escape', async () => {
    const plain = await button('plain');
    await plain.hover();

    await (await shown())!.close();

    expect(await shown()).toBeNull();
  });

  it('sets an explanation: what it explains, the explanation and a footnote', async () => {
    const explained = await button('explained');
    await explained.hover();
    const tip = (await shown())!;

    expect(await tip.getTitle()).toBe('Power');
    expect(await tip.getReason()).toBe('Raises every damage roll.');
    expect(await tip.getMeta()).toBe('From Strength');
    expect(await explained.getDescription()).toBe(
      'Power. Raises every damage roll. From Strength',
    );
  });

  it('with lgTooltipPin, a press pins it open and a second press closes it', fakeAsync(async () => {
    const explained = await button('explained');

    await explained.click();
    const tip = (await shown())!;
    expect(await tip.isPinned(explained)).toBeTrue();

    await explained.click();
    flush();
    expect(await shown()).toBeNull();
  }));

  it('without lgTooltipPin, a press does not pin it', fakeAsync(async () => {
    const plain = await button('plain');

    await plain.hover();
    await plain.click();
    const tip = (await shown())!;
    expect(await tip.isPinned(plain)).toBeFalse();
    flush();
  }));

  it('renders a template, described by lgTooltipDescription', async () => {
    const templated = await button('templated');
    await templated.hover();

    expect(document.querySelector('.lg-tip .from-template')?.textContent).toBe(
      'Hit',
    );
    expect(await templated.getDescription()).toBe('Hit chance and crit chance');
  });
});
