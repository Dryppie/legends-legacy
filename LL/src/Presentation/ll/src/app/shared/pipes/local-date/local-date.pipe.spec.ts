import { LocalDatePipe, resolveLocalDateLocale } from './local-date.pipe';

describe('LocalDatePipe', () => {
  const pipe = new LocalDatePipe();

  it('uses the resolved local locale and browser time zone', () => {
    const value = '2026-08-21T12:34:56Z';

    expect(pipe.transform(value, 'short')).toBe(
      new Intl.DateTimeFormat(resolveLocalDateLocale(), {
        dateStyle: 'short',
        timeStyle: 'short',
      }).format(new Date(value)),
    );
  });

  it('uses European English conventions for a US English browser in Europe', () => {
    expect(resolveLocalDateLocale('en-US', 'Europe/Copenhagen')).toBe('en-GB');
  });

  it('preserves explicitly configured regional locales', () => {
    expect(resolveLocalDateLocale('da-DK', 'Europe/Copenhagen')).toBe('da-DK');
    expect(resolveLocalDateLocale('en-US', 'America/New_York')).toBe('en-US');
  });

  it('returns null for missing or invalid dates', () => {
    expect(pipe.transform(null)).toBeNull();
    expect(pipe.transform('not-a-date')).toBeNull();
  });
});
