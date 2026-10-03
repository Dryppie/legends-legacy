import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgTagComponent, LgTagTone } from './tag.component';

@Component({
  imports: [LgTagComponent],
  template: `
    <lg-tag class="sc-state" state="expiring" value="2h" />
    <lg-tag class="sc-words" [tone]="tone()" [ariaHidden]="hidden()"
      >New quest</lg-tag
    >
  `,
})
class TagHost {
  readonly tone = signal<LgTagTone>('new');
  readonly hidden = signal(false);
}

describe('LgTagComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(TagHost);
    fixture.detectChanges();
    const q = (s: string) =>
      fixture.nativeElement.querySelector(s) as HTMLElement;
    return { fixture, q };
  }

  it('is its own box, toned on the host', () => {
    const { q } = setup();
    const tag = q('.sc-words');
    expect(tag.classList).toContain('lg-tag');
    expect(tag.classList).toContain('lg-tag--new');
    expect(getComputedStyle(tag).display).toBe('inline-flex');
    expect(tag.textContent?.trim()).toBe('New quest');
  });

  it('a state brings its word and glyph, and the value follows', () => {
    const { q } = setup();
    const tag = q('.sc-state');
    expect(tag.classList).toContain('lg-tag--is-expiring');
    expect(tag.querySelector('.lg-tag__value')?.textContent).toBe('2h');
  });

  it('a harmful condition says so to screen readers; ariaHidden hides the Tag', () => {
    const { fixture, q } = setup();
    fixture.componentInstance.tone.set('harmful');
    fixture.componentInstance.hidden.set(true);
    fixture.detectChanges();
    const tag = q('.sc-words');
    expect(tag.querySelector('.lg-sr')?.textContent).toBe(', harmful');
    expect(tag.getAttribute('aria-hidden')).toBe('true');
  });
});
