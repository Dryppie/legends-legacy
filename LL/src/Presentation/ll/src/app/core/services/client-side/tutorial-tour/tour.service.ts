// shared/help/tour.service.ts
import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { Driver, driver, DriveStep, Side } from 'driver.js';

export interface TourStepJSON {
  element: string; // CSS selector
  title?: string;
  description: string;
  position?: Side; // 'left' | 'right' | …
  navigateTo: string;
}

@Injectable({ providedIn: 'root' })
export class TourService {
  private currentStepIndex = 0;
  private steps: TourStepJSON[] = [];
  private drv: Driver | null = null;

  constructor(private router: Router) {}

  async start(pageId: string) {
    if (localStorage.getItem(`tour:${pageId}`) === 'done') return;

    this.steps = await fetch(`/assets/help/tours/${pageId}.json`)
      .then((r) => (r.ok ? r.json() : []))
      .catch(() => []);

    if (!this.steps.length) return;

    this.drv = driver({
      animate: true,
      showProgress: true,
      nextBtnText: 'Next',
      prevBtnText: 'Back',
      doneBtnText: 'Finish',
      stagePadding: 4,
    });

    this.drv.setConfig({
      onNextClick: () => this.handleNextClick(),
    });

    this.drv.setSteps(
      this.steps.map<DriveStep>((s) => ({
        element: s.element,
        popover: {
          title: s.title,
          description: s.description,
          side: s.position ?? 'bottom',
          align: 'center',
        },
      })),
    );

    this.drv.drive();
  }

  private async handleNextClick() {
    const step = this.steps[this.currentStepIndex];
    const nextStep = this.steps[this.currentStepIndex + 1];

    this.currentStepIndex++;

    if (step?.navigateTo && nextStep) {
      this.drv?.destroy(); // stop current tour
      await this.router.navigateByUrl(step.navigateTo);

      // Wait for the next route and DOM to render before restarting
      setTimeout(() => {
        this.startTourFrom(this.currentStepIndex);
      }, 500); // adjust delay if necessary
    }
  }

  private startTourFrom(index: number) {
    const valid = this.steps
      .slice(index)
      .filter((s) => document.querySelector(s.element));

    if (!valid.length) return;

    this.currentStepIndex = index;

    this.drv = driver({
      animate: true,
      showProgress: true,
      nextBtnText: 'Next',
      prevBtnText: 'Back',
      doneBtnText: 'Finish',
      stagePadding: 4,
    });

    this.drv.setConfig({
      onNextClick: () => this.handleNextClick(),
    });

    this.drv.setSteps(
      valid.map<DriveStep>((s) => ({
        element: s.element,
        popover: {
          title: s.title,
          description: s.description,
          side: s.position ?? 'bottom',
          align: 'center',
        },
      })),
    );

    this.drv.drive();
  }
}
