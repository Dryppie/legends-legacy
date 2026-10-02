import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import {
  LgConstellationComponent,
  LgConstellationItem,
  LgConstellationRing,
} from './constellation.component';
import { LgConstellationHarness } from '../../testing/constellation.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [LgConstellationComponent],
  template: `
    <lg-constellation
      [items]="items"
      [rings]="rings"
      [nodes]="nodes"
      [selectedId]="star()"
      (select)="star.set($event); selects.push($event)"
    />
  `,
})
class ConstellationHost {
  readonly items: LgConstellationItem[] = [
    { id: 'str', label: 'Strength', value: 24, x: 300, y: 200 },
    {
      id: 'dex',
      label: 'Dexterity',
      value: 18,
      x: 600,
      y: 300,
      labelPosition: 'left',
      size: 'sm',
      state: 'ready',
    },
    {
      id: 'int',
      label: 'Intellect',
      value: 9,
      x: 500,
      y: 500,
      state: 'locked',
      reason: 'Unlocks at level 30',
    },
  ];
  readonly rings: LgConstellationRing[] = [
    { cx: 500, cy: 350, r: 200 },
    { cx: 500, cy: 350, r: 300, strong: true },
  ];
  readonly nodes = [
    { x: 700, y: 350 },
    { x: 500, y: 50 },
  ];
  readonly star = signal('str');
  /** Every `select` the chart emitted. */
  readonly selects: string[] = [];
}

describe('LgConstellationComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [ConstellationHost] });
    const fixture = TestBed.createComponent(ConstellationHost);
    const chart = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgConstellationHarness,
    );
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    /** The reason tip while it shows, or null. */
    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
    return { host: fixture.componentInstance, chart, shownTip };
  }

  /** The parity scenario's opening: a click on Dexterity. */
  async function clickDexterity() {
    const ctx = await setup();
    await ctx.chart.click('Dexterity');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('starts with the selected Sigil pressed and as its one tab stop', fakeAsync(async () => {
    const { chart } = await setup();

    expect(await chart.getSigilLabels()).toEqual([
      'Strength',
      'Dexterity',
      'Intellect',
    ]);
    expect(await chart.selectedLabels()).toEqual(['Strength']);
    expect(await chart.tabStopLabels()).toEqual(['Strength']);
    expect(await chart.isLocked('Intellect')).toBeTrue();
  }));

  it('a click on a Sigil selects it and focuses it', fakeAsync(async () => {
    const { host, chart, shownTip } = await clickDexterity();

    expect(host.selects).toEqual(['dex']);
    expect(await chart.selectedLabels()).toEqual(['Dexterity']);
    expect(await chart.focusedLabel()).toBe('Dexterity');
    expect(await chart.tabStopLabels()).toEqual(['Dexterity']);
    expect(await shownTip()).toBeNull();
  }));

  it('Right lands on a locked Sigil without selecting anything, and its reason opens', fakeAsync(async () => {
    const { host, chart, shownTip } = await clickDexterity();

    await chart.pressKey('ArrowRight');

    expect(await chart.focusedLabel()).toBe('Intellect');
    expect(await chart.tabStopLabels()).toEqual(['Intellect']);
    expect(host.selects).toEqual(['dex']);
    expect(await chart.selectedLabels()).toEqual(['Dexterity']);
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Unlocks at level 30');
  }));

  it('Enter on a locked Sigil pins and announces its reason instead of selecting it', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, chart, shownTip } = await clickDexterity();
    await chart.pressKey('ArrowRight');

    await chart.pressKey('Enter');

    expect(host.selects).toEqual(['dex']);
    expect(await chart.selectedLabels()).toEqual(['Dexterity']);
    expect(await chart.focusedLabel()).toBe('Intellect');
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Unlocks at level 30');
    const intellect = {
      hover: () => chart.hover('Intellect'),
      mouseAway: () => chart.mouseAway('Intellect'),
    };
    expect(await tip?.isPinned(intellect)).toBeTrue();
    expect(watch.said).toEqual(['polite: Locked. Unlocks at level 30']);

    flush();
    watch.stop();
  }));

  it('Home moves focus to the first Sigil, closes the reason and keeps the selection', fakeAsync(async () => {
    const { host, chart, shownTip } = await clickDexterity();
    await chart.pressKey('ArrowRight');
    await chart.pressKey('Enter');

    await chart.pressKey('Home');

    expect(await chart.focusedLabel()).toBe('Strength');
    expect(await chart.tabStopLabels()).toEqual(['Strength']);
    expect(await shownTip()).toBeNull();
    expect(host.selects).toEqual(['dex']);
    expect(await chart.selectedLabels()).toEqual(['Dexterity']);

    flush();
  }));
});
