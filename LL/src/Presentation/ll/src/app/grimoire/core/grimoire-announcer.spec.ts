import { TestBed, fakeAsync, flush, tick } from '@angular/core/testing';
import { LgAnnouncer } from './grimoire-announcer';
import {
  lgAnnouncerIdle,
  lgQuietAnnouncer,
  lgWatchAnnouncements,
} from '../testing/announcer';

describe('LgAnnouncer', () => {
  let announcer: LgAnnouncer;

  beforeEach(lgAnnouncerIdle);
  beforeEach(() => (announcer = TestBed.inject(LgAnnouncer)));

  it('says a line politely, through the live region', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    announcer.announce('Locked. Unlocks at level 20');
    tick(100);

    expect(watch.said).toEqual(['polite: Locked. Unlocks at level 20']);
    watch.stop();
    flush();
  }));

  it('says polite lines one at a time, at most every 1.5s', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    announcer.announce('First');
    announcer.announce('Second');
    tick(100);
    expect(watch.said).toEqual(['polite: First']);
    tick(1400);
    expect(watch.said).toEqual(['polite: First']);
    tick(200);
    expect(watch.said).toEqual(['polite: First', 'polite: Second']);
    watch.stop();
    flush();
  }));

  it('replaces a waiting line with a newer one under the same key', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    announcer.announce('Now');
    announcer.announce('Loot: 1 Wolf Pelt', { key: 'loot' });
    announcer.announce('Loot: 3 Wolf Pelts', { key: 'loot' });
    tick(1600);

    expect(watch.said).toEqual(['polite: Now', 'polite: Loot: 3 Wolf Pelts']);
    watch.stop();
    flush();
  }));

  it('does not repeat the same words under the same key within 5s', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    announcer.announce('Short by 250 Cinders', { key: 'why' });
    tick(2000);
    announcer.announce('Short by 250 Cinders', { key: 'why' });
    tick(2000);
    expect(watch.said).toEqual(['polite: Short by 250 Cinders']);

    tick(1500);
    announcer.announce('Short by 250 Cinders', { key: 'why' });
    tick(100);
    expect(watch.said).toEqual([
      'polite: Short by 250 Cinders',
      'polite: Short by 250 Cinders',
    ]);
    watch.stop();
    flush();
  }));

  it('keeps at most three lines waiting, dropping the oldest', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    ['One', 'Two', 'Three', 'Four', 'Five'].forEach((t) =>
      announcer.announce(t),
    );
    tick(10000);

    expect(watch.said).toEqual([
      'polite: One',
      'polite: Three',
      'polite: Four',
      'polite: Five',
    ]);
    watch.stop();
    flush();
  }));

  it('says an error assertively, at once', fakeAsync(() => {
    lgQuietAnnouncer();
    const watch = lgWatchAnnouncements();

    announcer.announce('Waiting line');
    tick(100);
    announcer.announce('The market is closed', { assertive: true });
    tick(100);

    expect(watch.said).toEqual([
      'polite: Waiting line',
      'assertive: The market is closed',
    ]);
    watch.stop();
    flush();
  }));
});
