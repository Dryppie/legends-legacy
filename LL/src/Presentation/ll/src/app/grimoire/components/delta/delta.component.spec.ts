import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgDeltaComponent } from './delta.component';

@Component({
  imports: [LgDeltaComponent],
  template: `
    <lg-delta class="sc-up" direction="up" value="12%" polarity="better" />
    <lg-delta class="sc-none" direction="none" value="0" polarity="neutral" />
  `,
})
class DeltaHost {}

describe('LgDeltaComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(DeltaHost);
    fixture.detectChanges();
    return (s: string) => fixture.nativeElement.querySelector(s) as HTMLElement;
  }

  it('is its own box, coloured by polarity on the host, with a class of the caller’s beside it', () => {
    const up = setup()('.sc-up');
    expect(up.classList).toContain('lg-delta');
    expect(up.classList).toContain('lg-delta--better');
    expect(up.classList).toContain('sc-up');
    expect(up.textContent?.replace(/\s+/g, ' ').trim()).toBe('▲+12%, better');
  });

  it('no change reads as unchanged', () => {
    const none = setup()('.sc-none');
    expect(none.querySelector('.lg-sr')?.textContent).toBe(', unchanged');
    expect(none.querySelector('.lg-delta__glyph')).toBeNull();
  });
});
