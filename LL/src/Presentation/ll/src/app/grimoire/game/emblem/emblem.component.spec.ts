import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgEmblemComponent } from './emblem.component';

@Component({
  imports: [LgEmblemComponent],
  template: `<lg-emblem [points]="6" [size]="96" />`,
})
class EmblemHost {}

describe('LgEmblemComponent', () => {
  it('is its own box, its size square, the drawing hidden from screen readers and its marks solid', () => {
    const fixture = TestBed.createComponent(EmblemHost);
    fixture.detectChanges();
    const emblem: HTMLElement =
      fixture.nativeElement.querySelector('lg-emblem');
    expect(emblem.getBoundingClientRect().width).toBe(96);
    expect(emblem.getBoundingClientRect().height).toBe(96);
    const svg = emblem.querySelector('svg')!;
    expect(svg.getAttribute('aria-hidden')).toBe('true');
    expect(emblem.querySelectorAll('.lg-emblem__dot').length).toBe(6);
    expect(
      getComputedStyle(emblem.querySelector('.lg-emblem__heart')!).fill,
    ).not.toBe('none');
    expect(
      getComputedStyle(emblem.querySelector('.lg-emblem__ring')!).fill,
    ).toBe('none');
  });
});
