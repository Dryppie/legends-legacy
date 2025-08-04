// shared/help/tour.service.ts
import { Injectable } from '@angular/core';
import { Alignment, driver, DriveStep, Side } from 'driver.js';
import { LocalStorageService } from '../local-storage/local-storage.service';

export interface TourStepJSON {
  element: string; // CSS selector
  title?: string;
  description: string;
  position?: Side; // 'left' | 'right' | …
  alignment?: Alignment;
}

@Injectable({ providedIn: 'root' })
export class TourService {
  constructor(private readonly storage: LocalStorageService) {}
  /** Kick off a tour whose JSON lives at /assets/help/tours/<pageId>.json */
  async start(pageId: string) {
    if (this.storage.get(`tour:${pageId}`) === 'done') return;

    const steps: TourStepJSON[] = await fetch(
      `/assets/help/tours/${pageId}.json`,
    ).then((r) => (r.ok ? r.json() : []));

    const valid = steps.filter((s) => !!document.querySelector(s.element));
    if (!valid.length) return;

    const drv = driver({
      animate: true,
      smoothScroll: true,
      showProgress: true,
      allowClose: false,
      nextBtnText: 'Next',
      prevBtnText: 'Back',
      doneBtnText: 'Finish',
      stagePadding: 4,
      popoverClass: 'driverjs-theme',
      onDestroyStarted: () => {
        drv.destroy();
        this.storage.set(`tour:${pageId}`, 'done');
      },
    });

    drv.setSteps(
      valid.map<DriveStep>((s) => ({
        element: s.element,
        popover: {
          title: s.title,
          description: s.description,
          side: s.position ?? 'bottom',
          align: s.alignment ?? 'center',
        },
      })),
    );

    drv.drive();
  }
}
