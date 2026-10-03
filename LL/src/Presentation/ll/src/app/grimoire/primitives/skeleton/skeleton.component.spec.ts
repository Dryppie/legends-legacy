import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgSkeletonComponent } from './skeleton.component';

@Component({
  imports: [LgSkeletonComponent],
  template: `
    <lg-skeleton class="one" />
    <lg-skeleton class="lines" count="3" />
    <lg-skeleton class="rows" shape="rows" />
    <lg-skeleton class="block" shape="block" width="4rem" height="5rem" />
  `,
})
class SkeletonHost {}

describe('LgSkeletonComponent', () => {
  function setup(): HTMLElement {
    const fixture = TestBed.createComponent(SkeletonHost);
    fixture.detectChanges();
    return fixture.nativeElement;
  }
  const bars = (el: HTMLElement, which: string) =>
    el.querySelectorAll(`lg-skeleton.${which} .lg-skeleton__bar`).length;

  it('draws lines of text, rows or a block, in the shape of what is coming', () => {
    const el = setup();
    expect(bars(el, 'one')).toBe(1);
    expect(bars(el, 'lines')).toBe(3);
    expect(bars(el, 'rows')).toBe(3);
    expect(bars(el, 'block')).toBe(0);
    const block = el.querySelector<HTMLElement>('lg-skeleton.block')!;
    expect(block.style.width).toBe('4rem');
    expect(block.style.height).toBe('5rem');
  });

  it('is for the eye only: screen readers hear the region’s busy state and words instead', () => {
    const el = setup();
    el.querySelectorAll('lg-skeleton').forEach((s) =>
      expect(s.getAttribute('aria-hidden')).toBe('true'),
    );
  });
});
