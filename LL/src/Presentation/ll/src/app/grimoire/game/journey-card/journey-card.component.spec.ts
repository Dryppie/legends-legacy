import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  LgJourneyCardActionsComponent,
  LgJourneyCardComponent,
} from './journey-card.component';

@Component({
  imports: [LgJourneyCardComponent, LgJourneyCardActionsComponent],
  template: `
    <lg-journey-card
      [stages]="['First Hunt', 'Shenic Journey', 'Journey Complete']"
      phase="Shenic Journey"
      heading="Develop your Shenic build"
      objective="Choose a current quest."
      nextUnlock="A third Essence slot at level 20"
    >
      <lg-journey-card-actions
        ><button type="button">Open World Map</button></lg-journey-card-actions
      >
    </lg-journey-card>
  `,
})
class JourneyHost {}

describe('LgJourneyCardComponent', () => {
  it('is a region named by its heading, its actions under the objective and its stages a Track', () => {
    const fixture = TestBed.createComponent(JourneyHost);
    fixture.detectChanges();
    const card: HTMLElement =
      fixture.nativeElement.querySelector('lg-journey-card');
    expect(card.getAttribute('role')).toBe('region');
    expect(
      document.getElementById(card.getAttribute('aria-labelledby')!)
        ?.textContent,
    ).toBe('Develop your Shenic build');
    expect(card.hasAttribute('title')).toBeFalse();
    expect(
      card.querySelector('.lg-journey__main > lg-journey-card-actions button'),
    ).not.toBeNull();
    expect(
      card
        .querySelector('.lg-journey__track lg-track [role=progressbar]')
        ?.getAttribute('aria-valuetext'),
    ).toBe('Shenic Journey');
  });
});
