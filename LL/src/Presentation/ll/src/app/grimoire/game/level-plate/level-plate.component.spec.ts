import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgLevelPlateComponent } from './level-plate.component';

@Component({
  imports: [LgLevelPlateComponent],
  template: `<lg-level-plate [level]="17" [xp]="2400" [xpMax]="6000" />`,
})
class PlateHost {}

describe('LgLevelPlateComponent', () => {
  it('is its own box: the level, and its progress as a thicker experience Meter', () => {
    const fixture = TestBed.createComponent(PlateHost);
    fixture.detectChanges();
    const plate: HTMLElement =
      fixture.nativeElement.querySelector('lg-level-plate');
    expect(getComputedStyle(plate).display).toBe('grid');
    expect(plate.querySelector('.lg-levelplate__num')?.textContent).toBe('17');
    const meter = plate.querySelector('lg-meter') as HTMLElement;
    expect(meter.classList).toContain('lg-meter--xp');
    expect(
      getComputedStyle(meter.querySelector('.lg-meter__track')!).height,
    ).toBe('6px');
    expect(
      meter.querySelector('[role=meter]')?.getAttribute('aria-label'),
    ).toBe('Experience');
  });
});
