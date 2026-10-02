import { Component, signal } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgEntry, LgEntryListComponent } from './entry-list.component';
import { LgEntryListHarness } from '../../testing/entry-list.harness';
import {
  LgReasonTipHarness,
  lgCloseReasonTip,
} from '../../testing/reason-tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [LgEntryListComponent],
  template: `<lg-entry-list [items]="entries" [(activeId)]="entry" />`,
})
class EntryListHost {
  readonly entries: LgEntry[] = [
    { id: 'wolf', name: 'Ember Wolf', tag: 'New' },
    { id: 'slime', name: 'Blue Slime' },
    {
      id: 'drake',
      name: 'Ash Drake',
      locked: true,
      reason: 'Defeat the Ash Drake in Shenic',
    },
    { id: 'imp', name: 'Cinder Imp', tag: 'Rare', tagTone: 'rare' },
  ];
  readonly entry = signal('wolf');
}

describe('LgEntryListComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [EntryListHost] });
    const fixture = TestBed.createComponent(EntryListHost);
    const list =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgEntryListHarness,
      );
    const page = TestbedHarnessEnvironment.documentRootLoader(fixture);
    /** The reason tip while it shows, or null. */
    const shownTip = () =>
      page.getHarnessOrNull(LgReasonTipHarness.with({ shown: true }));
    return { host: fixture.componentInstance, list, shownTip };
  }

  /** The parity scenario's opening: a click on Blue Slime. */
  async function clickBlueSlime() {
    const ctx = await setup();
    await ctx.list.click('Blue Slime');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  beforeEach(lgCloseReasonTip);
  afterEach(lgCloseReasonTip);

  it('starts with the active entry as its one tab stop', fakeAsync(async () => {
    const { list } = await setup();

    expect(await list.selectedName()).toBe('Ember Wolf');
    expect(await list.tabStopNames()).toEqual(['Ember Wolf']);
    expect(await list.isLocked('Ash Drake')).toBeTrue();
  }));

  it('a click selects the entry and focuses it', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    expect(host.entry()).toBe('slime');
    expect(await list.selectedName()).toBe('Blue Slime');
    expect(await list.focusedName()).toBe('Blue Slime');
    expect(await list.tabStopNames()).toEqual(['Blue Slime']);
    expect(await shownTip()).toBeNull();
  }));

  it('Down lands on a locked entry without selecting it, and its reason opens beside it', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    await list.pressKey('ArrowDown');

    expect(await list.focusedName()).toBe('Ash Drake');
    expect(await list.tabStopNames()).toEqual(['Ash Drake']);
    expect(host.entry()).toBe('slime');
    expect(await list.selectedName()).toBe('Blue Slime');
    const tip = await shownTip();
    expect(await tip?.getWord()).toBe('Locked');
    expect(await tip?.getReason()).toBe('Defeat the Ash Drake in Shenic');
  }));

  it('Down past a locked entry selects the next one and closes the reason', fakeAsync(async () => {
    const { host, list, shownTip } = await clickBlueSlime();

    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');

    expect(await list.focusedName()).toBe('Cinder Imp');
    expect(host.entry()).toBe('imp');
    expect(await list.selectedName()).toBe('Cinder Imp');
    expect(await list.tabStopNames()).toEqual(['Cinder Imp']);
    expect(await shownTip()).toBeNull();
  }));

  it('Home jumps to the first entry and selects it', fakeAsync(async () => {
    const { host, list } = await clickBlueSlime();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');

    await list.pressKey('Home');

    expect(await list.focusedName()).toBe('Ember Wolf');
    expect(host.entry()).toBe('wolf');
    expect(await list.selectedName()).toBe('Ember Wolf');
    expect(await list.tabStopNames()).toEqual(['Ember Wolf']);
  }));

  it('Up from the first entry wraps to the last and selects it', fakeAsync(async () => {
    const { host, list } = await clickBlueSlime();
    await list.pressKey('ArrowDown');
    await list.pressKey('ArrowDown');
    await list.pressKey('Home');

    await list.pressKey('ArrowUp');

    expect(await list.focusedName()).toBe('Cinder Imp');
    expect(host.entry()).toBe('imp');
    expect(await list.selectedName()).toBe('Cinder Imp');
    expect(await list.tabStopNames()).toEqual(['Cinder Imp']);
  }));

  it('Enter on a locked entry pins and announces its reason, and the selection stays', fakeAsync(async () => {
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
    expect(await list.selectedName()).toBe('Cinder Imp');
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
});
