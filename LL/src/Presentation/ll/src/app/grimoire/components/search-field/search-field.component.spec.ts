import { Component, computed, signal } from '@angular/core';
import { TestBed, fakeAsync, flush } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgSearchFieldComponent } from './search-field.component';
import { LgSearchFieldHarness } from '../../testing/search-field.harness';
import { lgCloseReasonTip } from '../../testing/reason-tip.harness';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../../testing/announcer';

@Component({
  imports: [LgSearchFieldComponent],
  template: `
    <div (keydown.escape)="escapesAround = escapesAround + 1">
      <lg-search-field
        [(value)]="query"
        [suggestions]="suggestions()"
        searched
        (pick)="picks.push($event)"
        (submitted)="submits.push($event)"
      />
    </div>
  `,
})
class SearchFieldHost {
  readonly query = signal('');
  private readonly names = ['Maren', 'Marek', 'Mara', 'Kaelen'];
  readonly suggestions = computed(() => {
    const v = this.query().toLowerCase();
    return v
      ? this.names.filter((name) => name.toLowerCase().startsWith(v))
      : [];
  });
  readonly picks: string[] = [];
  readonly submits: string[] = [];
  /** Escape presses that reached the page around the field. */
  escapesAround = 0;
}

describe('LgSearchFieldComponent', () => {
  async function setup() {
    TestBed.configureTestingModule({ imports: [SearchFieldHost] });
    const fixture = TestBed.createComponent(SearchFieldHost);
    const field =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgSearchFieldHarness,
      );
    return { host: fixture.componentInstance, field };
  }

  /** The parity scenario's opening: a click in the field, then "Ma". */
  async function typeMa() {
    const ctx = await setup();
    await ctx.field.click();
    await ctx.field.type('Ma');
    return ctx;
  }

  beforeEach(lgAnnouncerIdle);
  // The reason tip hears Escape before anything else on the page: one another test left open would take this Escape.
  beforeEach(lgCloseReasonTip);

  it('starts empty and closed', fakeAsync(async () => {
    const { field } = await setup();

    expect(await field.getValue()).toBe('');
    expect(await field.isOpen()).toBeFalse();
  }));

  it('typing opens the matching suggestions, none highlighted, and says how many', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { host, field } = await typeMa();

    expect(host.query()).toBe('Ma');
    expect(await field.getValue()).toBe('Ma');
    expect(await field.isOpen()).toBeTrue();
    expect(await field.getSuggestions()).toEqual(['Maren', 'Marek', 'Mara']);
    expect(await field.highlightedSuggestion()).toBeNull();
    expect(await field.isFocused()).toBeTrue();
    expect(watch.said).toEqual(['polite: 3 suggestions']);

    flush();
    watch.stop();
  }));

  it('Down moves the highlight through the suggestions while focus stays in the field', fakeAsync(async () => {
    const { field } = await typeMa();

    await field.pressKey('ArrowDown');
    await field.pressKey('ArrowDown');

    expect(await field.highlightedSuggestion()).toBe('Marek');
    expect(await field.isOpen()).toBeTrue();
    expect(await field.isFocused()).toBeTrue();
    expect(await field.getValue()).toBe('Ma');
  }));

  it('Enter picks the highlighted suggestion and closes the list', fakeAsync(async () => {
    const { host, field } = await typeMa();
    await field.pressKey('ArrowDown');
    await field.pressKey('ArrowDown');

    await field.pressKey('Enter');

    expect(host.picks).toEqual(['Marek']);
    expect(host.submits).toEqual([]);
    expect(host.query()).toBe('Marek');
    expect(await field.getValue()).toBe('Marek');
    expect(await field.isOpen()).toBeFalse();
    expect(await field.isFocused()).toBeTrue();
  }));

  it('a search that matches nothing opens the list on "No matching players" and says so', fakeAsync(async () => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();
    const { field } = await typeMa();
    await field.pressKey('ArrowDown');
    await field.pressKey('ArrowDown');
    await field.pressKey('Enter');

    await field.type('x');

    expect(await field.getValue()).toBe('Marekx');
    expect(await field.isOpen()).toBeTrue();
    expect(await field.getSuggestions()).toEqual([]);
    expect(await field.getNote()).toBe('No matching players');
    expect(watch.said).toEqual([
      'polite: 3 suggestions',
      'polite: No matching players',
    ]);

    flush();
    watch.stop();
  }));

  it('Escape closes the list and goes no further', fakeAsync(async () => {
    const { host, field } = await typeMa();
    await field.pressKey('ArrowDown');
    await field.pressKey('ArrowDown');
    await field.pressKey('Enter');
    await field.type('x');

    await field.pressKey('Escape');

    expect(await field.isOpen()).toBeFalse();
    expect(await field.getValue()).toBe('Marekx');
    expect(host.escapesAround).toBe(0);
  }));

  it('Enter with nothing highlighted submits what was typed', fakeAsync(async () => {
    const { host, field } = await typeMa();
    await field.pressKey('ArrowDown');
    await field.pressKey('ArrowDown');
    await field.pressKey('Enter');
    await field.type('x');
    await field.pressKey('Escape');

    await field.pressKey('Enter');

    expect(host.submits).toEqual(['Marekx']);
    expect(host.picks).toEqual(['Marek']);
    expect(await field.isOpen()).toBeFalse();
    expect(await field.getValue()).toBe('Marekx');
  }));
});
