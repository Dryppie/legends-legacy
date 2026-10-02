import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LG_LIST, LgListRowState } from './list.component';
import { LgButtonComponent } from '../../primitives/button/button.component';
import { LgTagComponent } from '../../primitives/tag/tag.component';
import { LgListHarness } from '../../testing/list.harness';
import { LgTipHarness, lgCloseTip } from '../../testing/tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

/** A list of things to act on: each row's action selects it; A and B have a Sell action at their end. */
@Component({
  imports: [...LG_LIST, LgButtonComponent],
  template: `
    <lg-list label="Inventory" [(selected)]="row">
      <li lgListRow name="A" key="a" [amount]="1">
        <button lgListRowAction (click)="activated.push('a')"></button>
        <lg-list-row-trailing>
          <button lgButton (click)="sells.push('A')">Sell</button>
        </lg-list-row-trailing>
      </li>
      <li lgListRow name="B" key="b" [amount]="2">
        <button lgListRowAction (click)="activated.push('b')"></button>
        <lg-list-row-trailing>
          <button lgButton (click)="sells.push('B')">Sell</button>
        </lg-list-row-trailing>
      </li>
      <li
        lgListRow
        name="C"
        key="c"
        [amount]="3"
        [state]="cState()"
        reason="Sold out"
      >
        <button lgListRowAction (click)="activated.push('c')"></button>
      </li>
    </lg-list>

    <lg-list label="Guild members">
      <li lgListRow name="Maren" amount="9,915">
        <lg-list-row-trailing>
          <button lgButton>Invite</button>
        </lg-list-row-trailing>
      </li>
      <li lgListRow name="Kaelen" amount="8,204">
        <lg-list-row-trailing>
          <button lgButton>Invite</button>
        </lg-list-row-trailing>
      </li>
    </lg-list>
  `,
})
class ListHost {
  readonly row = signal<string | null>('b');
  readonly cState = signal<LgListRowState | undefined>(undefined);
  /** Every row whose action ran, in order. */
  readonly activated: string[] = [];
  /** Every Sell press, by row. */
  readonly sells: string[] = [];
}

/** A selectable scene list, as the creature browser uses it. */
@Component({
  imports: [...LG_LIST, LgTagComponent],
  template: `
    <lg-list label="Creatures" variant="scene" selectable [(selected)]="entry">
      <li lgListRow name="Ember Wolf" key="wolf">
        <lg-tag tone="new">New</lg-tag>
      </li>
      <li lgListRow name="Blue Slime" key="slime" ready="1 point to spend"></li>
      <li
        lgListRow
        name="Ash Drake"
        key="drake"
        state="locked"
        reason="Defeat the Ash Drake in Shenic"
        ready
      ></li>
      <li lgListRow name="Cinder Imp" key="imp">
        <lg-tag tone="rare">Rare</lg-tag>
      </li>
    </lg-list>
  `,
})
class SceneHost {
  readonly entry = signal('wolf');
}

describe('LgListComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [ListHost] });
    const fixture = TestBed.createComponent(ListHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const list = await loader.getHarness(
      LgListHarness.with({ label: 'Inventory' }),
    );
    const members = await loader.getHarness(
      LgListHarness.with({ label: 'Guild members' }),
    );
    return { fixture, host: fixture.componentInstance, list, members };
  }

  /** The parity scenario's opening: a click on row A. */
  async function clickA() {
    const ctx = await setup();
    await ctx.list.click('A');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('is one tab stop, on the selected row; trailing actions leave the Tab order', fakeAsync(async () => {
    const { list } = await setup();

    expect(await list.getRole()).toBe('list');
    expect(await list.getRowNames()).toEqual(['A', 'B', 'C']);
    expect(await list.selectedNames()).toEqual(['B']);
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'row' }]);
  }));

  it('names each action by its row', fakeAsync(async () => {
    const { fixture } = await setup();
    const action = fixture.nativeElement.querySelector(
      '.lg-listrow__action',
    ) as HTMLElement;
    const ids = action.getAttribute('aria-labelledby')!.split(' ');

    expect(ids.map((id) => document.getElementById(id)?.textContent)).toEqual([
      'A',
    ]);
    expect(action.getAttribute('aria-pressed')).toBe('false');
  }));

  it('a click on a row runs its action, selects it and focuses it', fakeAsync(async () => {
    const { host, list } = await clickA();

    expect(host.activated).toEqual(['a']);
    expect(host.row()).toBe('a');
    expect(await list.selectedNames()).toEqual(['A']);
    expect(await list.getFocus()).toEqual({ row: 'A', on: 'row' });
    expect(await list.getTabStops()).toEqual([{ row: 'A', on: 'row' }]);
  }));

  it('Down moves focus to the next row without running its action', fakeAsync(async () => {
    const { host, list } = await clickA();

    await list.pressKey('ArrowDown');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'row' });
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'row' }]);
    expect(host.activated).toEqual(['a']);
    expect(await list.selectedNames()).toEqual(['A']);
  }));

  it("Right moves into the row's trailing action without pressing it", fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');

    await list.pressKey('ArrowRight');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'trailing' });
    expect(await list.getTabStops()).toEqual([{ row: 'B', on: 'row' }]);
    expect(host.sells).toEqual([]);
    expect(await list.selectedNames()).toEqual(['A']);
  }));

  it('Left moves back from the trailing action to the row', fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');

    await list.pressKey('ArrowLeft');

    expect(await list.getFocus()).toEqual({ row: 'B', on: 'row' });
    expect(host.sells).toEqual([]);
    expect(await list.selectedNames()).toEqual(['A']);
  }));

  it('End jumps to the last row; Down there stays', fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');
    await list.pressKey('ArrowLeft');

    await list.pressKey('End');
    expect(await list.getFocus()).toEqual({ row: 'C', on: 'row' });
    await list.pressKey('ArrowDown');

    expect(await list.getFocus()).toEqual({ row: 'C', on: 'row' });
    expect(await list.getTabStops()).toEqual([{ row: 'C', on: 'row' }]);
    expect(host.activated).toEqual(['a']);
    expect(await list.selectedNames()).toEqual(['A']);
  }));

  it('Enter runs the focused row’s action and selects it; no trailing action ran', fakeAsync(async () => {
    const { host, list } = await clickA();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowRight');
    await list.pressKey('ArrowLeft');
    await list.pressKey('End');

    await list.pressKey('Enter');

    expect(host.activated).toEqual(['a', 'c']);
    expect(await list.selectedNames()).toEqual(['C']);
    expect(await list.getFocus()).toEqual({ row: 'C', on: 'row' });
    expect(host.sells).toEqual([]);
  }));

  it('Up from a trailing action moves to the row above', fakeAsync(async () => {
    const { list } = await setup();
    await list.click('B');
    await list.pressKey('ArrowRight');

    await list.pressKey('ArrowUp');

    expect(await list.getFocus()).toEqual({ row: 'A', on: 'row' });
  }));

  it('typing a name’s first letters moves to that row', fakeAsync(async () => {
    const { list } = await clickA();

    await list.type('c');
    tick(200);

    expect(await list.getFocus()).toEqual({ row: 'C', on: 'row' });
    expect(await list.getTabStops()).toEqual([{ row: 'C', on: 'row' }]);
  }));

  it('a blocked row stays in reach, but its action neither runs nor selects; a press says why', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { fixture, host, list } = await clickA();
    host.cState.set('unavailable');
    fixture.detectChanges();

    await list.pressKey('End');
    await list.pressKey('Enter');

    expect(await list.getFocus()).toEqual({ row: 'C', on: 'row' });
    expect(await list.blockedNames()).toEqual(['C']);
    expect(host.activated).toEqual(['a']);
    expect(await list.selectedNames()).toEqual(['A']);
    expect(await (await list.getRow('C')).getDescription()).toBe('Sold out');
    expect(watch.said).toEqual(['polite: Sold out']);

    flush();
    watch.stop();
  }));

  it('a row without an action is out of the keys’ reach: its trailing controls are ordinary tab stops', fakeAsync(async () => {
    const { members } = await setup();

    expect(await members.getTabStops()).toEqual([
      { row: 'Maren', on: 'trailing' },
      { row: 'Kaelen', on: 'trailing' },
    ]);
    expect(await members.selectedNames()).toEqual([]);
  }));

  it('shares its columns: an amount column appears when any row has one', fakeAsync(async () => {
    const { fixture } = await setup();
    const ul = fixture.nativeElement.querySelector('ul') as HTMLElement;

    expect(ul.style.gridTemplateColumns).toBe('minmax(0px, 1fr) auto auto');
  }));
});

describe('LgListComponent, selectable scene list', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [SceneHost] });
    const fixture = TestBed.createComponent(SceneHost);
    const list =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(LgListHarness);
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    /** The reason tip while it shows, or null. */
    const shownTip = () =>
      page.getHarnessOrNull(LgTipHarness.with({ shown: true }));
    return { fixture, host: fixture.componentInstance, list, shownTip };
  }

  /** The parity scenario's opening: a click on Blue Slime. */
  async function clickBlueSlime() {
    const ctx = await setup();
    await ctx.list.click('Blue Slime');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseTip);
  afterEach(lgCloseTip);

  it('starts with the selected row as its one tab stop', fakeAsync(async () => {
    const { list } = await setup();

    expect(await list.getRole()).toBe('listbox');
    expect(await list.selectedNames()).toEqual(['Ember Wolf']);
    expect(await list.getTabStops()).toEqual([
      { row: 'Ember Wolf', on: 'row' },
    ]);
    expect(await list.blockedNames()).toEqual(['Ash Drake']);
  }));

  it('a click selects the row and focuses it', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    expect(host.entry()).toBe('slime');
    expect(await list.selectedNames()).toEqual(['Blue Slime']);
    expect(await list.focusedName()).toBe('Blue Slime');
    expect(await list.getTabStops()).toEqual([
      { row: 'Blue Slime', on: 'row' },
    ]);
    expect(await shownTip()).toBeNull();
  }));

  it('Down lands on a locked row without selecting it, and its reason opens beside it', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    await list.pressKey('ArrowDown');

    expect(await list.focusedName()).toBe('Ash Drake');
    expect(await list.getTabStops()).toEqual([{ row: 'Ash Drake', on: 'row' }]);
    expect(host.entry()).toBe('slime');
    expect(await list.selectedNames()).toEqual(['Blue Slime']);
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Defeat the Ash Drake in Shenic');
  }));

  it('Down past a locked row selects the next one and closes the reason', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');

    expect(await list.focusedName()).toBe('Cinder Imp');
    expect(host.entry()).toBe('imp');
    expect(await list.selectedNames()).toEqual(['Cinder Imp']);
    expect(await list.getTabStops()).toEqual([
      { row: 'Cinder Imp', on: 'row' },
    ]);
    expect(await shownTip()).toBeNull();
  }));

  it('Home jumps to the first row and selects it', fakeAsync(async () => {
    const { host, list } = await clickBlueSlime();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');

    await list.pressKey('Home');

    expect(await list.focusedName()).toBe('Ember Wolf');
    expect(host.entry()).toBe('wolf');
    expect(await list.selectedNames()).toEqual(['Ember Wolf']);
    expect(await list.getTabStops()).toEqual([
      { row: 'Ember Wolf', on: 'row' },
    ]);
  }));

  it('Up from the first row wraps to the last and selects it', fakeAsync(async () => {
    const { host, list } = await clickBlueSlime();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');
    await list.pressKey('Home');

    await list.pressKey('ArrowUp');

    expect(await list.focusedName()).toBe('Cinder Imp');
    expect(host.entry()).toBe('imp');
    expect(await list.selectedNames()).toEqual(['Cinder Imp']);
    expect(await list.getTabStops()).toEqual([
      { row: 'Cinder Imp', on: 'row' },
    ]);
  }));

  it('Enter on a locked row pins and announces its reason, and the selection stays', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, list, shownTip } = await clickBlueSlime();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');
    await list.pressKey('Home');
    await list.pressKey('ArrowUp');

    await list.pressKey('ArrowUp');
    await list.pressKey('Enter');

    expect(await list.focusedName()).toBe('Ash Drake');
    expect(host.entry()).toBe('imp');
    expect(await list.selectedNames()).toEqual(['Cinder Imp']);
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Defeat the Ash Drake in Shenic');
    const drake = {
      hover: () => list.hover('Ash Drake'),
      mouseAway: () => list.mouseAway('Ash Drake'),
    };
    expect(await tip?.isPinned(drake)).toBeTrue();
    expect(watch.said).toEqual([
      'polite: Locked. Defeat the Ash Drake in Shenic',
    ]);

    flush();
    watch.stop();
  }));

  it('typing a name’s first letters moves to that row and selects it', fakeAsync(async () => {
    const { host, list } = await clickBlueSlime();

    await list.type('ci');
    tick(200);

    expect(await list.focusedName()).toBe('Cinder Imp');
    expect(host.entry()).toBe('imp');
  }));

  it('a locked row shows its state’s Tag in place of its own, and takes no attention diamond', fakeAsync(async () => {
    const { fixture } = await setup();
    const rows = fixture.nativeElement.querySelectorAll(
      '.lg-listrow',
    ) as NodeListOf<HTMLElement>;
    const shown = (row: HTMLElement, sel: string) =>
      Array.from(row.querySelectorAll<HTMLElement>(sel)).filter(
        (el) => !el.closest('[hidden]'),
      ).length;

    expect(shown(rows[1], '.lg-attention')).toBe(1);
    expect(rows[1].textContent).toContain('1 point to spend');
    expect(shown(rows[2], '.lg-attention')).toBe(0);
    expect(shown(rows[2], '.lg-tag--is-locked')).toBe(1);
    expect(rows[2].getAttribute('aria-describedby')).toBeTruthy();
  }));
});
