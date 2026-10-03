import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgMeterComponent } from './meter.component';

@Component({
  imports: [LgMeterComponent],
  template: `
    <lg-meter
      class="sc-hp"
      label="Health"
      [value]="84"
      [max]="120"
      unit="HP"
      size="bar"
      live
    />
    <div style="--lg-meter-height: 0.375rem">
      <lg-meter
        class="sc-xp"
        tone="xp"
        [value]="2"
        [max]="8"
        [showValue]="false"
        ariaLabel="EXP"
      />
    </div>
  `,
})
class MeterHost {}

describe('LgMeterComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(MeterHost);
    fixture.detectChanges();
    const q = (s: string) =>
      fixture.nativeElement.querySelector(s) as HTMLElement;
    return { q };
  }

  it('is its own box; its track is a meter with the value in words', () => {
    const { q } = setup();
    const hp = q('.sc-hp');
    expect(hp.classList).toContain('lg-meter--hp');
    expect(hp.classList).toContain('lg-meter--bar');
    expect(hp.classList).toContain('is-live');
    expect(getComputedStyle(hp).display).toBe('grid');
    const track = hp.querySelector('[role=meter]')!;
    expect(track.getAttribute('aria-valuetext')).toBe('84 of 120 HP');
    expect(track.getAttribute('aria-label')).toBe('Health');
  });

  it('a box around it sets its track height through --lg-meter-height', () => {
    const { q } = setup();
    const track = q('.sc-xp .lg-meter__track');
    expect(getComputedStyle(track).height).toBe('6px');
    expect(q('.sc-xp .lg-meter__head')).toBeNull();
  });
});
