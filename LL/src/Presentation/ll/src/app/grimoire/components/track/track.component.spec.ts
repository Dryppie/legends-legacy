import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgTrackComponent } from './track.component';

@Component({
  imports: [LgTrackComponent],
  template: `<lg-track
    [steps]="5"
    [current]="2"
    tone="hp"
    startLabel="Floor 3"
    endLabel="Boss"
    label="World Tower"
  />`,
})
class TrackHost {}

describe('LgTrackComponent', () => {
  it('is its own box; its rail is a progress bar with the step in words', () => {
    const fixture = TestBed.createComponent(TrackHost);
    fixture.detectChanges();
    const track: HTMLElement = fixture.nativeElement.querySelector('lg-track');
    expect(track.classList).toContain('lg-track--hp');
    expect(getComputedStyle(track).display).toBe('flex');
    const rail = track.querySelector('[role=progressbar]')!;
    expect(rail.getAttribute('aria-valuenow')).toBe('3');
    expect(rail.getAttribute('aria-valuetext')).toBe('Step 3 of 5');
    expect(rail.getAttribute('aria-label')).toBe('World Tower');
    expect(track.querySelectorAll('.lg-track__node.is-done').length).toBe(2);
    expect(track.querySelectorAll('.lg-track__node.is-current').length).toBe(1);
  });
});
