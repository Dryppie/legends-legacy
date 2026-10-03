import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  LgSectionRuleAsideComponent,
  LgSectionRuleComponent,
  LgSectionRuleVariant,
} from './section-rule.component';

@Component({
  imports: [LgSectionRuleComponent, LgSectionRuleAsideComponent],
  template: `
    <lg-section-rule [variant]="variant()" label="Status">
      <lg-section-rule-aside>3 of 5</lg-section-rule-aside>
    </lg-section-rule>
  `,
})
class RuleHost {
  readonly variant = signal<LgSectionRuleVariant>('band');
}

describe('LgSectionRuleComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(RuleHost);
    fixture.detectChanges();
    const rule: HTMLElement =
      fixture.nativeElement.querySelector('lg-section-rule');
    return { fixture, rule };
  }

  it('is the separator itself, named by its label, with its aside at the end', () => {
    const { rule } = setup();
    expect(rule.getAttribute('role')).toBe('separator');
    expect(rule.getAttribute('aria-label')).toBe('Status');
    expect(rule.classList).toContain('lg-rule--band');
    const aside = rule.querySelector('lg-section-rule-aside') as HTMLElement;
    expect(aside.classList).toContain('lg-rule__aside');
    expect(getComputedStyle(aside).marginLeft).not.toBe('0px');
  });

  it('a hairline puts the aside after its line', () => {
    const { fixture, rule } = setup();
    fixture.componentInstance.variant.set('hairline');
    fixture.detectChanges();
    const aside = rule.querySelector('lg-section-rule-aside') as HTMLElement;
    expect(getComputedStyle(aside).order).toBe('2');
    expect(getComputedStyle(aside).marginLeft).toBe('0px');
  });

  it('the ornament is its lattice, without the aside', () => {
    const { fixture, rule } = setup();
    fixture.componentInstance.variant.set('ornament');
    fixture.detectChanges();
    expect(rule.className).toBe('lg-rule lg-rule--ornament');
    expect(rule.querySelectorAll('.lg-rule__lattice').length).toBe(2);
    expect(rule.querySelector('lg-section-rule-aside')).toBeNull();
  });
});
