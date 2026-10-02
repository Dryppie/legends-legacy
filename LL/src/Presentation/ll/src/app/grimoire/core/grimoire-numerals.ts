/*
 * Numeral pipes (Foundations · Numerals, D-135): the format helpers in templates. They replace `lg-num`.
 *
 *   {{ 12480 | lgNumber }}              12,480        {{ 1.1 | lgNumber: 2 }}   1.10
 *   {{ 12480 | lgShort }}               12.5k
 *   {{ 24.8 | lgPercent: 1 }}           24.8%
 *   {{ 84 | lgUnit: 'HP/5s' }}          84 HP/5s (a non-breaking space; symbol units attach: 12s)
 *   {{ 252 | lgDuration }}              4m 12s        {{ 252 | lgDuration: 'spoken' }}   4 minutes 12 seconds
 *
 * A unit set smaller and muted after its number takes markup, so `lgValue` splits a value for it. Keep the number and
 * the unit's span touching: whitespace between them would show as a space (and a block such as @if around the span
 * makes Prettier put a line break there). Without a unit the span is empty, and `.lg-unit:empty` takes no room:
 *
 *   @let v = value | lgValue;
 *   <b>{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></b>
 */
import { Pipe, PipeTransform } from '@angular/core';
import {
  LgNumeric,
  lgFormatDuration,
  lgFormatNumber,
  lgFormatPercent,
  lgFormatShort,
  lgFormatUnit,
  lgPlainValue,
  lgSpokenDuration,
  lgUnitSplit,
} from './grimoire-format';

/** 12,480 · −12 (a true minus) · — for unknown · a fixed number of decimals when given. */
@Pipe({ name: 'lgNumber' })
export class LgNumberPipe implements PipeTransform {
  transform(value: LgNumeric, digits?: number): string {
    return lgFormatNumber(value, digits);
  }
}

/** 12480 → 12.5k, 3200000 → 3.2M. Only where the full figure is also read out (Foundations · Numerals). */
@Pipe({ name: 'lgShort' })
export class LgShortPipe implements PipeTransform {
  transform(value: LgNumeric): string {
    return lgFormatShort(value);
  }
}

/** 24.8% */
@Pipe({ name: 'lgPercent' })
export class LgPercentPipe implements PipeTransform {
  transform(value: LgNumeric, digits?: number): string {
    return lgFormatPercent(value, digits);
  }
}

/** A symbol unit attaches (24.8%, 12s); a word unit takes a non-breaking space (84 HP/5s). */
@Pipe({ name: 'lgUnit' })
export class LgUnitPipe implements PipeTransform {
  transform(value: LgNumeric, unit: string, digits?: number): string {
    return lgFormatUnit(value, unit, digits);
  }
}

/** Two units at most: 4m 12s, 2h 14m. `'spoken'` writes it for screen readers: 4 minutes 12 seconds. */
@Pipe({ name: 'lgDuration' })
export class LgDurationPipe implements PipeTransform {
  transform(seconds: LgNumeric, form: 'written' | 'spoken' = 'written'): string {
    return form === 'spoken' ? lgSpokenDuration(seconds) : lgFormatDuration(seconds);
  }
}

/** A value split for display: its number, and its unit (or null) to set smaller and muted after it. */
export interface LgValueParts {
  number: string;
  unit: string | null;
}

/**
 * Splits a value for display as a number and a unit: "84 HP/5s" → 84 and HP/5s. A number gets thousands separators and
 * a true minus; an unknown value is —. Signs stay whole: a range, a multiplier and a fraction are one number.
 */
@Pipe({ name: 'lgValue' })
export class LgValuePipe implements PipeTransform {
  transform(value: LgNumeric): LgValueParts {
    const split = lgUnitSplit(value);
    return split ? { number: split.number, unit: split.unit } : { number: lgPlainValue(value), unit: null };
  }
}

/** Every numeral pipe, for a standalone `imports` array. */
export const LG_NUMERAL_PIPES = [
  LgNumberPipe,
  LgShortPipe,
  LgPercentPipe,
  LgUnitPipe,
  LgDurationPipe,
  LgValuePipe,
] as const;
