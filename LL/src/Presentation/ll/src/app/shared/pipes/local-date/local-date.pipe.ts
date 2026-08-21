import { Pipe, PipeTransform } from '@angular/core';

export type LocalDateFormat =
  | 'short'
  | 'medium'
  | 'mediumDate'
  | 'shortTime'
  | 'mediumTime';

const FORMAT_OPTIONS: Record<LocalDateFormat, Intl.DateTimeFormatOptions> = {
  short: {
    dateStyle: 'short',
    timeStyle: 'short',
  },
  medium: {
    dateStyle: 'medium',
    timeStyle: 'medium',
  },
  mediumDate: {
    dateStyle: 'medium',
  },
  shortTime: {
    timeStyle: 'short',
  },
  mediumTime: {
    timeStyle: 'medium',
  },
};

export function resolveLocalDateLocale(
  browserLocale: string | undefined =
    typeof navigator === 'undefined' ? undefined : navigator.language,
  timeZone: string | undefined = new Intl.DateTimeFormat().resolvedOptions()
    .timeZone,
): string | undefined {
  if (!browserLocale || !timeZone?.startsWith('Europe/')) {
    return browserLocale;
  }

  const locale = new Intl.Locale(browserLocale);
  return locale.language === 'en' && locale.region === 'US'
    ? 'en-GB'
    : browserLocale;
}

export function formatLocalDate(
  value: string | number | Date | null | undefined,
  format: LocalDateFormat = 'medium',
): string | null {
  if (value === null || value === undefined || value === '') return null;

  const date = value instanceof Date ? value : new Date(value);
  if (Number.isNaN(date.getTime())) return null;

  return new Intl.DateTimeFormat(
    resolveLocalDateLocale(),
    FORMAT_OPTIONS[format] ?? FORMAT_OPTIONS.medium,
  ).format(date);
}

@Pipe({
  name: 'localDate',
  standalone: true,
})
export class LocalDatePipe implements PipeTransform {
  transform(
    value: string | number | Date | null | undefined,
    format: LocalDateFormat = 'medium',
  ): string | null {
    return formatLocalDate(value, format);
  }
}
