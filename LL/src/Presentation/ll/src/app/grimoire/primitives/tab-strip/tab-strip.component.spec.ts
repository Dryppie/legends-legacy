import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgTab, LgTabStripComponent } from './tab-strip.component';
import { LgTabStripHarness } from '../../testing/tab-strip.harness';

@Component({
  imports: [LgTabStripComponent],
  template: `<lg-tab-strip [tabs]="tabs" [(activeId)]="tab" />`,
})
class TabStripHost {
  readonly tabs: LgTab[] = [
    { id: 'all', label: 'All', count: 12 },
    { id: 'weapons', label: 'Weapons' },
    { id: 'armor', label: 'Armor', count: 0 },
  ];
  readonly tab = signal('all');
}

describe('LgTabStripComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [TabStripHost] });
    const fixture = TestBed.createComponent(TabStripHost);
    const strip =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgTabStripHarness,
      );
    return { host: fixture.componentInstance, strip };
  }

  it('starts with the active tab selected and as its one tab stop', fakeAsync(async () => {
    const { strip } = await setup();

    expect(await strip.getTabLabels()).toEqual(['All', 'Weapons', 'Armor']);
    expect(await strip.selectedLabel()).toBe('All');
    expect(await strip.tabStopLabels()).toEqual(['All']);
  }));

  it('a click selects the tab and focuses it', fakeAsync(async () => {
    const { host, strip } = await setup();

    await strip.click('Weapons');

    expect(host.tab()).toBe('weapons');
    expect(await strip.selectedLabel()).toBe('Weapons');
    expect(await strip.focusedLabel()).toBe('Weapons');
    expect(await strip.tabStopLabels()).toEqual(['Weapons']);
  }));

  it('Right moves focus to the next tab and selects it', fakeAsync(async () => {
    const { host, strip } = await setup();
    await strip.click('Weapons');

    await strip.pressKey('ArrowRight');

    expect(host.tab()).toBe('armor');
    expect(await strip.selectedLabel()).toBe('Armor');
    expect(await strip.focusedLabel()).toBe('Armor');
    expect(await strip.tabStopLabels()).toEqual(['Armor']);
  }));

  it('Right on the last tab wraps to the first', fakeAsync(async () => {
    const { host, strip } = await setup();
    await strip.click('Weapons');
    await strip.pressKey('ArrowRight');

    await strip.pressKey('ArrowRight');

    expect(host.tab()).toBe('all');
    expect(await strip.selectedLabel()).toBe('All');
    expect(await strip.focusedLabel()).toBe('All');
    expect(await strip.tabStopLabels()).toEqual(['All']);
  }));
});
