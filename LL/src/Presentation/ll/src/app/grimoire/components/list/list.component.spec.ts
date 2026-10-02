import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgListComponent, LgListRowComponent } from './list.component';
import { LgButtonComponent } from '../../primitives/button/button.component';
import { LgSlotDirective } from '../../core/grimoire-core';
import { LgListHarness } from '../../testing/list.harness';

@Component({
  imports: [
    LgListComponent,
    LgListRowComponent,
    LgButtonComponent,
    LgSlotDirective,
  ],
  template: `
    <lg-list label="Inventory">
      <li
        lgListRow
        title="A"
        [value]="1"
        interactive
        [selected]="row() === 'a'"
        (activate)="activate('a')"
      >
        <button lgButton lgSlot="trailing" (click)="sells.push('A')">
          Sell
        </button>
      </li>
      <li
        lgListRow
        title="B"
        [value]="2"
        interactive
        [selected]="row() === 'b'"
        (activate)="activate('b')"
      >
        <button lgButton lgSlot="trailing" (click)="sells.push('B')">
          Sell
        </button>
      </li>
      <li
        lgListRow
        title="C"
        [value]="3"
        interactive
        [selected]="row() === 'c'"
        (activate)="activate('c')"
      ></li>
    </lg-list>
  `,
})
class ListHost {
  readonly row = signal('b');
  /** Every row that emitted `activate`, in order. */
  readonly activated: string[] = [];
  /** Every Sell press, by row. */
  readonly sells: string[] = [];

  activate(row: string): void {
    this.activated.push(row);
    this.row.set(row);
  }
}

describe('LgListComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [ListHost] });
    const fixture = TestBed.createComponent(ListHost);
    const list = await TestbedHarnessEnvironment.loader(fixture).getHarness(
      LgListHarness.with({ label: 'Inventory' }),
    );
    return { host: fixture.componentInstance, list };
  }

  /** The parity scenario's opening: a click on row A's name. */
  async function clickA() {
    const ctx = await setup();
    await ctx.list.click('A');
    return ctx;
  }

  it('is one tab stop, on the selected row; trailing actions leave the Tab order', fakeAsync(async () => {
    const { list } = await setup();

    expect(await list.getRowTitles()).toEqual(['A', 'B', 'C']);
    expect(await list.selectedTitles()).toEqual(['B']);
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'name' }]);
  }));

  it("a click on a row's name activates it and focuses it", fakeAsync(async () => {
    const { host, list } = await clickA();

    expect(host.activated).toEqual(['a']);
    expect(await list.selectedTitles()).toEqual(['A']);
    expect(await list.getFocus()).toEqual({ row: 'A', on: 'name' });
    expect(await list.getTabStops()).toEqual([{ row: 'A', on: 'name' }]);
  }));

  it('Down moves focus to the next row without activating it', fakeAsync(async () => {
    const { host, list } = await clickA();

    await list.pressKey('ArrowDown');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'name' });
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'name' }]);
    expect(host.activated).toEqual(['a']);
    expect(await list.selectedTitles()).toEqual(['A']);
  }));

  it("Right moves into the row's trailing action without pressing it", fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');

    await list.pressKey('ArrowRight');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'action' });
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'name' }]);
    expect(host.sells).toEqual([]);
    expect(await list.selectedTitles()).toEqual(['A']);
  }));

  it("Left moves back from the trailing action to the row's name", fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');

    await list.pressKey('ArrowLeft');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'name' });
    expect(host.sells).toEqual([]);
    expect(await list.selectedTitles()).toEqual(['A']);
  }));

  it('End jumps to the last row', fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');
    await list.pressKey('ArrowLeft');

    await list.pressKey('End');

    expect(await list.getFocus()).toEqual({ row: 'C', on: 'name' });
    expect(await list.getTabStops()).toEqual([{ row: 'C', on: 'name' }]);
    expect(host.activated).toEqual(['a']);
    expect(await list.selectedTitles()).toEqual(['A']);
  }));

  it('Enter activates the focused row; no trailing action ran', fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');
    await list.pressKey('ArrowLeft');
    await list.pressKey('End');

    await list.pressKey('Enter');

    expect(host.activated).toEqual(['a', 'c']);
    expect(await list.selectedTitles()).toEqual(['C']);
    expect(await list.getFocus()).toEqual({ row: 'C', on: 'name' });
    expect(host.sells).toEqual([]);
  }));
});
