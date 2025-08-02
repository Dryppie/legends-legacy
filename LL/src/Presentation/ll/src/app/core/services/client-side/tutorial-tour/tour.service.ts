// shared/help/tour.service.ts
import { Injectable } from '@angular/core';
import { driver, DriveStep, Side } from 'driver.js';

export interface TourStepJSON {
  element: string; // CSS selector
  title?: string;
  description: string;
  position?: Side; // 'left' | 'right' | …
}

@Injectable({ providedIn: 'root' })
export class TourService {
  /** Kick off a tour whose JSON lives at /assets/help/tours/<pageId>.json */
  async start(pageId: string) {
    if (localStorage.getItem(`tour:${pageId}`) === 'done') return;

    const steps: TourStepJSON[] = await fetch(
      `/assets/help/tours/${pageId}.json`,
    ).then((r) => (r.ok ? r.json() : []));

    const valid = steps.filter((s) => !!document.querySelector(s.element));
    if (!valid.length) return;

    const drv = driver({
      animate: true,
      smoothScroll: true,
      showProgress: true,
      nextBtnText: 'Next',
      prevBtnText: 'Back',
      doneBtnText: 'Finish',
      stagePadding: 4,
      popoverClass: 'driverjs-theme',
    });

    drv.setSteps(
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

    // drv.on('destroy', () => localStorage.setItem(`tour:${pageId}`, 'done'));

    drv.drive();
  }
}
