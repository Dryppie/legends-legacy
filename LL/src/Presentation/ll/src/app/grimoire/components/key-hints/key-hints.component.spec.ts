import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgKeyHintsComponent } from './key-hints.component';

@Component({
  imports: [LgKeyHintsComponent],
  template: `<lg-key-hints [hints]="hints" />`,
})
class HintsHost {
  readonly hints = [
    { label: 'Back', key: 'Esc' },
    { label: 'Travel', key: '↵' },
  ];
}

describe('LgKeyHintsComponent', () => {
  it('is a named group of hints, each its words then its key cap', () => {
    const fixture = TestBed.createComponent(HintsHost);
    fixture.detectChanges();
    const hints: HTMLElement =
      fixture.nativeElement.querySelector('lg-key-hints');
    expect(hints.getAttribute('role')).toBe('group');
    expect(hints.getAttribute('aria-label')).toBe('Keyboard shortcuts');
    const keys = Array.from(
      hints.querySelectorAll('kbd.lg-key'),
      (k) => k.textContent,
    );
    expect(keys).toEqual(['Esc', '↵']);
    expect(hints.querySelector('.lg-keyhints__item')?.textContent).toBe(
      'BackEsc',
    );
  });
});
