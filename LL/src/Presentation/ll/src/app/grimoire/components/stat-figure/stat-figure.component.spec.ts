import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgStatFigureComponent } from './stat-figure.component';

@Component({
  imports: [LgStatFigureComponent],
  template: `<lg-stat-figure
    label="Combat rating"
    [value]="1840"
    caption="All sources"
    size="sm"
    title="Summed from your attributes"
  />`,
})
class FigureHost {}

describe('LgStatFigureComponent', () => {
  it('is its own box; its explanation is the host’s own title', () => {
    const fixture = TestBed.createComponent(FigureHost);
    fixture.detectChanges();
    const figure: HTMLElement =
      fixture.nativeElement.querySelector('lg-stat-figure');
    expect(figure.classList).toContain('lg-figure--sm');
    expect(getComputedStyle(figure).display).toBe('grid');
    expect(figure.getAttribute('title')).toBe('Summed from your attributes');
    expect(figure.querySelector('.lg-figure__value')?.textContent).toBe(
      '1,840',
    );
    expect(figure.querySelector('.lg-figure__caption')?.textContent).toBe(
      'All sources',
    );
  });
});
