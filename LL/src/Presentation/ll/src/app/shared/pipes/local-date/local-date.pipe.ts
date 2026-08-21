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

@Pipe({
  name: 'localDate',
  standalone: true,
})
export class LocalDatePipe implements PipeTransform {
  transform(
    value: string | number | Date | null | undefined,
    format: LocalDateFormat = 'medium',
  ): string | null {
    if (value === null || value === undefined || value === '') return null;

    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return null;

    return new Intl.DateTimeFormat(
      undefined,
      FORMAT_OPTIONS[format] ?? FORMAT_OPTIONS.medium,
    ).format(date);
  }
}
