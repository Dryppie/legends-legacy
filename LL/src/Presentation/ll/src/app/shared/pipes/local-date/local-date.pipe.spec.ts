import { LocalDatePipe } from './local-date.pipe';

describe('LocalDatePipe', () => {
  const pipe = new LocalDatePipe();

  it('uses the browser locale and time zone', () => {
    const value = '2026-08-21T12:34:56Z';

    expect(pipe.transform(value, 'short')).toBe(
      new Intl.DateTimeFormat(undefined, {
        dateStyle: 'short',
        timeStyle: 'short',
      }).format(new Date(value)),
    );
  });

  it('returns null for missing or invalid dates', () => {
    expect(pipe.transform(null)).toBeNull();
    expect(pipe.transform('not-a-date')).toBeNull();
  });
});
