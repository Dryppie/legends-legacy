import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  LG_NUMERAL_PIPES,
  LgDurationPipe,
  LgNumberPipe,
  LgPercentPipe,
  LgShortPipe,
  LgUnitPipe,
  LgValuePipe,
} from './grimoire-numerals';
import { LG_NBSP, LgNumeric } from './grimoire-format';

describe('Grimoire numeral pipes', () => {
  it('lgNumber: thousands, a true minus, fixed decimals, — for unknown', () => {
    const p = new LgNumberPipe();
    expect(p.transform(12480)).toBe('12,480');
    expect(p.transform(-12)).toBe('−12');
    expect(p.transform(1.1, 2)).toBe('1.10');
    expect(p.transform(0)).toBe('0');
    expect(p.transform(null)).toBe('—');
  });

  it('lgShort, lgPercent, lgUnit', () => {
    expect(new LgShortPipe().transform(12480)).toBe('12.5k');
    expect(new LgPercentPipe().transform(24.8, 1)).toBe('24.8%');
    expect(new LgUnitPipe().transform(12, 's')).toBe('12s');
    expect(new LgUnitPipe().transform(84, 'HP/5s')).toBe(`84${LG_NBSP}HP/5s`);
  });

  it('lgDuration: two units at most, or spoken for screen readers', () => {
    const p = new LgDurationPipe();
    expect(p.transform(252)).toBe('4m 12s');
    expect(p.transform(252, 'spoken')).toBe('4 minutes 12 seconds');
  });

  it('lgValue: a number and its unit, signs whole, — for unknown', () => {
    const p = new LgValuePipe();
    expect(p.transform('84 HP/5s')).toEqual({
      number: '84',
      unit: `${LG_NBSP}HP/5s`,
    });
    expect(p.transform('24.8%')).toEqual({ number: '24.8', unit: '%' });
    expect(p.transform(12480)).toEqual({ number: '12,480', unit: null });
    expect(p.transform('12–18')).toEqual({ number: '12–18', unit: null });
    expect(p.transform(null)).toEqual({ number: '—', unit: null });
    expect(p.transform('')).toEqual({ number: '—', unit: null });
  });

  @Component({
    imports: [...LG_NUMERAL_PIPES],
    template: `
      @let v = value() | lgValue;
      <span class="out"
        >{{ v.number }}<span class="lg-unit">{{ v.unit }}</span></span
      >
    `,
  })
  class ValueCase {
    readonly value = signal<LgNumeric>('84 HP/5s');
  }

  it('lgValue in a template sets the unit after its number, with nothing between them, and an empty unit without one', () => {
    const fixture = TestBed.createComponent(ValueCase);
    fixture.detectChanges();
    const out = (fixture.nativeElement as HTMLElement).querySelector('.out')!;

    expect(out.textContent).toBe(`84${LG_NBSP}HP/5s`);
    expect(out.querySelector('.lg-unit')?.textContent).toBe(`${LG_NBSP}HP/5s`);

    fixture.componentInstance.value.set(1284);
    fixture.detectChanges();
    expect(out.textContent).toBe('1,284');
    expect(out.querySelector('.lg-unit')?.textContent).toBe('');
  });
});
