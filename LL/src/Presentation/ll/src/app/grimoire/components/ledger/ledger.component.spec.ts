import { Component } from '@angular/core';
import { TestBed, fakeAsync } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgLedgerComponent, LgLedgerRow } from './ledger.component';
import { LgLedgerHarness } from '../../testing/ledger.harness';
import { lgCloseReasonTip } from '../../testing/reason-tip.harness';

@Component({
  imports: [LgLedgerComponent],
  template: `<lg-ledger title="Offense" [rows]="rows" />`,
})
class LedgerHost {
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
    TestBed.configureTestingModule({ imports: [LedgerHost] });
    const fixture = TestBed.createComponent(LedgerHost);
    const ledger = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgLedgerHarness.with({ title: 'Offense' }),
    );
    return { ledger };
  }

  /** The parity scenario's opening: a click on Power. */
  async function clickPower() {
    const ctx = await setup();
    await ctx.ledger.click('Power');
    return ctx;
  }

  // The reason tip hears Escape before anything else on the page: one another test left open would take this Escape.
  beforeEach(lgCloseReasonTip);

  it('makes the rows that explain themselves one tab stop, on the first of them', fakeAsync(async () => {
    const { ledger } = await setup();

    expect(await ledger.getRowLabels()).toEqual([
      'Power',
      'Crit Chance',
      'Health Regen',
      'Range',
      'Magic Penetration',
      'Style',
    ]);
    expect(await ledger.tabStopLabels()).toEqual(['Power']);
    expect(await ledger.pinnedLabels()).toEqual([]);
  }));

  it('a click on a row pins its explanation and focuses it', fakeAsync(async () => {
    const { ledger } = await clickPower();

    expect(await ledger.pinnedLabels()).toEqual(['Power']);
    expect(await ledger.focusedLabel()).toBe('Power');
    expect(await ledger.tabStopLabels()).toEqual(['Power']);
  }));

  it('Escape unpins the explanation and focus stays on the row', fakeAsync(async () => {
    const { ledger } = await clickPower();

    await ledger.pressKey('Escape');

    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.dismissedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBe('Power');
  }));

  it('Down skips rows without an explanation and moves to the next that has one', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');

    await ledger.pressKey('ArrowDown');

    expect(await ledger.focusedLabel()).toBe('Health Regen');
    expect(await ledger.tabStopLabels()).toEqual(['Health Regen']);
    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.dismissedLabels()).toEqual([]);
  }));

  it('Escape on a focused row with nothing pinned puts its explanation away', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');
    await ledger.pressKey('ArrowDown');

    await ledger.pressKey('Escape');

    expect(await ledger.dismissedLabels()).toEqual(['Health Regen']);
    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBe('Health Regen');
  }));

  it('a click on a row whose explanation was put away pins it again', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');
    await ledger.pressKey('ArrowDown');
    await ledger.pressKey('Escape');

    await ledger.click('Health Regen');

    expect(await ledger.pinnedLabels()).toEqual(['Health Regen']);
    expect(await ledger.dismissedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBe('Health Regen');
  }));

  it('a press anywhere else closes the pinned explanation', fakeAsync(async () => {
    const { ledger } = await clickPower();
    await ledger.pressKey('Escape');
    await ledger.pressKey('ArrowDown');
    await ledger.pressKey('Escape');
    await ledger.click('Health Regen');

    pressOutside();

    expect(await ledger.pinnedLabels()).toEqual([]);
    expect(await ledger.dismissedLabels()).toEqual([]);
    expect(await ledger.focusedLabel()).toBeNull();
    expect(await ledger.tabStopLabels()).toEqual(['Health Regen']);
  }));
});
