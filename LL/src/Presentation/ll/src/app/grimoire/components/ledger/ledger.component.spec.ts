import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgDeltaPolarity } from '../delta/delta.component';
import { LG_LEDGER, LgLedgerRow } from './ledger.component';
import { LgLedgerHarness } from '../../testing/ledger.harness';
import { lgCloseTip } from '../../testing/tip.harness';

@Component({
  imports: [...LG_LEDGER],
  // Kept in view: the tip closes when its element is off screen.
  host: { style: 'position: fixed; top: 0; left: 0; width: 20rem' },
  template: `
    <lg-ledger heading="Offense">
      @for (row of rows; track row.label) {
        <div
          lgLedgerRow
          [label]="row.label"
          [value]="row.value"
          [sub]="row.sub"
          [description]="row.description"
          [tipMeta]="row.tipMeta"
          [muted]="!!row.muted"
        ></div>
      }
    </lg-ledger>
    <lg-ledger heading="Defenses" [columns]="columns()">
      <div lgLedgerRow label="Armor" value="45"></div>
      <div
        lgLedgerRow
        label="Resist"
        value="30"
        [delta]="4"
        [deltaPolarity]="polarity()"
      ></div>
      <div
        lgLedgerRow
        label="Cooldown"
        value="6.8s"
        [delta]="-1.2"
        deltaPolarity="better"
      ></div>
      <div lgLedgerRow label="Block" value="5%" [delta]="0"></div>
      <div
        lgLedgerRow
        label="Power"
        value="154"
        [delta]="12"
        deltaPolarity="better"
        sub="now 142"
      ></div>
    </lg-ledger>
  `,
})
class LedgerHost {
  readonly columns = signal(1);
  readonly polarity = signal<LgDeltaPolarity>('better');
  readonly rows: LgLedgerRow[] = [
    {
      label: 'Power',
      value: 1284,
      description: 'How hard you hit.',
      tipMeta: 'From equipment',
    },
    { label: 'Crit Chance', value: '24.8%', sub: '+3% from Essences' },
    {
      label: 'Health Regen',
      value: '84 HP/5s',
      description: 'Health restored every five seconds.',
    },
    { label: 'Range', value: '12–18' },
    { label: 'Magic Penetration', value: null, muted: true },
    {
      label: 'Haste',
      value: '15',
      description: 'Cooldowns recover faster.',
    },
    { label: 'Style', value: 'Reaper' },
  ];
}

/** A press on empty page space: the pointer goes down outside every component, and focus leaves for the body. */
function pressOutside(): void {
  document.body.dispatchEvent(
    new PointerEvent('pointerdown', { bubbles: true }),
  );
  (document.activeElement as HTMLElement | null)?.blur();
  document.body.dispatchEvent(new MouseEvent('click', { bubbles: true }));
}

describe('LgLedgerComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(LedgerHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const ledger = await loader.getHarness(
      LgLedgerHarness.with({ heading: 'Offense' }),
    );
    const changes = await loader.getHarness(
      LgLedgerHarness.with({ heading: 'Defenses' }),
    );
    const host = fixture.nativeElement as HTMLElement;
    return { fixture, ledger, changes, host };
  }

  /** The opening of the click scenarios: a click on Power. */
  async function clickPower() {
    const ctx = await setup();
    await ctx.ledger.click('Power');
    return ctx;
  }

  // The tip hears Escape before anything else on the page: one another test left open would take this Escape.
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('is the box itself: a region named by its heading, holding a dl of term and definition pairs', async () => {
    const { host } = await setup();
    const ledger = host.querySelector<HTMLElement>('lg-ledger')!;
    const heading = ledger.querySelector('h3')!;
    expect(getComputedStyle(ledger).display).toBe('block');
    expect(ledger.getAttribute('role')).toBe('region');
    expect(ledger.getAttribute('aria-labelledby')).toBe(heading.id);
    expect(heading.textContent).toBe('Offense');
    expect(ledger.hasAttribute('title')).toBeFalse();

    const dl = ledger.querySelector('dl')!;
    const rows = Array.from(dl.children);
    expect(rows.length).toBe(7);
    for (const row of rows) {
      expect(row.tagName).toBe('DIV');
      expect(row.firstElementChild?.tagName).toBe('DT');
      expect(row.children[1].tagName).toBe('DD');
    }
  });

  it('reads each value with its unit, — for one that does not apply', async () => {
    const { ledger } = await setup();
    expect(await (await ledger.getRow('Power')).getValue()).toBe('1,284');
    expect(await (await ledger.getRow('Health Regen')).getValue()).toBe(
      '84 HP/5s',
    );
    expect(await (await ledger.getRow('Range')).getValue()).toBe('12–18');
    const missing = await ledger.getRow('Magic Penetration');
    expect(await missing.getValue()).toBe('—');
    expect(await missing.isMuted()).toBeTrue();
    expect(await (await ledger.getRow('Crit Chance')).getSub()).toBe(
      '+3% from Essences',
    );
  });

  it('makes the rows that explain themselves one tab stop, on the first of them', fakeAsync(async () => {
    const { ledger } = await setup();

    expect(await ledger.getRowLabels()).toEqual([
      'Power',
      'Crit Chance',
      'Health Regen',
      'Range',
      'Magic Penetration',
      'Haste',
      'Style',
    ]);
    expect(await ledger.tabStopLabels()).toEqual(['Power']);
    expect(await (await ledger.getRow('Range')).explains()).toBeFalse();
    expect(await ledger.pinnedLabels()).toEqual([]);
  }));

  it('gives each explaining row its explanation as its description, without repeating its label', async () => {
    const { ledger } = await setup();
    expect(await (await ledger.getRow('Power')).getDescription()).toBe(
      'How hard you hit. From equipment',
    );
    expect(await (await ledger.getRow('Range')).getDescription()).toBeNull();
  });

  it('a click on a row pins its explanation in the tip and focuses it', fakeAsync(async () => {
    const { ledger } = await clickPower();

    expect(await ledger.pinnedLabels()).toEqual(['Power']);
    expect(await ledger.explainedLabels()).toEqual(['Power']);
    expect(await ledger.focusedLabel()).toBe('Power');
    expect(await ledger.tabStopLabels()).toEqual(['Power']);
    expect(await ledger.getExplanation()).toEqual({
      title: 'Power',
      text: 'How hard you hit.',
      meta: 'From equipment',
    });
  }));

  it('Escape closes the pinned explanation and focus stays on the row', fakeAsync(async () => {
    const { ledger } = await clickPower();

    await ledger.pressKey('Escape');

    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.explainedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBe('Power');
  }));

  it('Down skips rows without an explanation and moves to the next that has one, which shows its own', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');

    await ledger.pressKey('ArrowDown');

    expect(await ledger.focusedLabel()).toBe('Health Regen');
    expect(await ledger.tabStopLabels()).toEqual(['Health Regen']);
    expect(await ledger.explainedLabels()).toEqual(['Health Regen']);
    expect(await ledger.pinnedLabels()).toEqual([]);
  }));

  it('Up, Home and End move between the rows that explain themselves, without wrapping', fakeAsync(async () => {
    const { ledger } = await clickPower();

    await ledger.pressKey('End');
    expect(await ledger.focusedLabel()).toBe('Haste');
    await ledger.pressKey('ArrowDown');
    expect(await ledger.focusedLabel()).toBe('Haste');
    await ledger.pressKey('ArrowUp');
    expect(await ledger.focusedLabel()).toBe('Health Regen');
    await ledger.pressKey('Home');
    expect(await ledger.focusedLabel()).toBe('Power');
    await ledger.pressKey('ArrowUp');
    expect(await ledger.focusedLabel()).toBe('Power');
  }));

  it('typing a label’s first letters moves to that row', fakeAsync(async () => {
    const { ledger } = await clickPower();

    await ledger.type('ha');
    tick(200);

    expect(await ledger.focusedLabel()).toBe('Haste');
    expect(await ledger.tabStopLabels()).toEqual(['Haste']);
  }));

  it('Escape on a focused row with nothing pinned puts its explanation away', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');
    await ledger.pressKey('ArrowDown');

    await ledger.pressKey('Escape');

    expect(await ledger.explainedLabels()).toEqual([]);
    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBe('Health Regen');
  }));

  it('a click on a row whose explanation was put away pins it again, and a second click closes it', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');
    await ledger.pressKey('ArrowDown');
    await ledger.pressKey('Escape');

    await ledger.click('Health Regen');

    expect(await ledger.pinnedLabels()).toEqual(['Health Regen']);
    expect(await ledger.focusedLabel()).toBe('Health Regen');

    await ledger.click('Health Regen');

    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.explainedLabels()).toEqual([]);
  }));

  it('a press anywhere else closes the pinned explanation; the tab stop stays where focus was', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('ArrowDown');
    await ledger.click('Health Regen');

    pressOutside();

    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.explainedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBeNull();
    expect(await ledger.tabStopLabels()).toEqual(['Health Regen']);
  }));

  it('hover shows a row’s explanation, and it doesn’t move off a pinned one', fakeAsync(async () => {
    const { ledger } = await setup();

    await (await ledger.getRow('Haste')).hover();
    expect(await ledger.explainedLabels()).toEqual(['Haste']);
    await (await ledger.getRow('Haste')).mouseAway();
    tick(200);
    expect(await ledger.explainedLabels()).toEqual([]);

    await ledger.click('Power');
    await (await ledger.getRow('Haste')).hover();
    expect(await ledger.explainedLabels()).toEqual(['Power']);
  }));

  it('shows a change under its value: glyph, sign and size in the value’s unit, coloured by its polarity', async () => {
    const { fixture, changes, host } = await setup();
    const resist = await changes.getRow('Resist');
    expect(await resist.getSub()).toContain('▲');
    expect(await resist.getSub()).toContain('+4');
    expect(await (await changes.getRow('Cooldown')).getSub()).toContain(
      '−1.2s',
    );
    expect(await (await changes.getRow('Block')).getSub()).toContain('±0');
    const power = await (await changes.getRow('Power')).getSub();
    expect(power).toContain('+12');
    expect(power).toContain('· now 142');

    const delta = () =>
      host.querySelectorAll('lg-ledger')[1].querySelector('.lg-delta')!;
    expect(delta().classList).toContain('lg-delta--better');
    fixture.componentInstance.polarity.set('worse');
    fixture.detectChanges();
    expect(delta().classList).toContain('lg-delta--worse');
    expect(await (await changes.getRow('Armor')).getSub()).toBeNull();
  });

  it('columns=2 sets the rows two-up, each column aligning its own values', async () => {
    const { fixture, changes, host } = await setup();
    expect(await changes.getColumns()).toBe(1);
    fixture.componentInstance.columns.set(2);
    fixture.detectChanges();
    expect(await changes.getColumns()).toBe(2);

    const dl = host.querySelectorAll('lg-ledger')[1].querySelector('dl')!;
    expect(getComputedStyle(dl).gridTemplateColumns.split(' ').length).toBe(6);
    const [armor, resist] = Array.from(dl.children) as HTMLElement[];
    expect(armor.getBoundingClientRect().top).toBe(
      resist.getBoundingClientRect().top,
    );
    expect(resist.getBoundingClientRect().left).toBeGreaterThan(
      armor.getBoundingClientRect().left,
    );
  });
});
