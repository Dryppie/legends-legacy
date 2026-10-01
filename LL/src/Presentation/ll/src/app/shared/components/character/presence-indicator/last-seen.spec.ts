import { lastSeenLabel } from './last-seen';

describe('lastSeenLabel', () => {
  const ago = (ms: number) => new Date(Date.now() - ms).toISOString();

  it('says how long ago, in the largest whole unit', () => {
    expect(lastSeenLabel(ago(10_000))).toBe('just now');
    expect(lastSeenLabel(ago(60_000))).toBe('1 minute ago');
    expect(lastSeenLabel(ago(3 * 3_600_000))).toBe('3 hours ago');
    expect(lastSeenLabel(ago(2 * 86_400_000))).toBe('2 days ago');
  });

  it('is null when the time is missing or unreadable', () => {
    expect(lastSeenLabel(null)).toBeNull();
    expect(lastSeenLabel('not a date')).toBeNull();
  });
});
