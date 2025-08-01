import { Injectable } from '@angular/core';
import Shepherd from 'shepherd.js';

export interface TourStepJSON {
  id: string;
  text: string | string[];
  title?: string;
  attachTo: {
    element: string;
    on:
      | 'top'
      | 'top-start'
      | 'top-end'
      | 'bottom'
      | 'bottom-start'
      | 'bottom-end'
      | 'left'
      | 'left-start'
      | 'left-end'
      | 'right'
      | 'right-start'
      | 'right-end'
      | 'auto'
      | 'auto-start'
      | 'auto-end';
  };
}

@Injectable({ providedIn: 'root' })
export class TourService {
  /** Run a tour whose JSON lives at assets/help/tours/<pageId>.json */
  async start(pageId: string) {
    // ⬐ 1. Load the JSON
    const steps = await fetch(`assets/help/tours/${pageId}.json`).then((r) =>
      r.ok ? r.json() : [],
    );

    if (!steps.length) return; // nothing to do

    // ⬐ 2. Build a Shepherd tour instance
    const tour = new Shepherd.Tour({
      useModalOverlay: true,
      defaultStepOptions: {
        scrollTo: true,
        canClickTarget: false,
        classes: 'shadow-xl bg-slate-800 text-white max-w-sm',
      },
    });

    // ⬐ 3. Add steps (skip those whose elements aren’t in DOM)
    steps.forEach((s: TourStepJSON) => {
      if (!document.querySelector(s.attachTo.element)) return;
      tour.addStep({
        ...s,
        buttons: [
          {
            text: 'Back',
            action: tour.back,
            secondary: true,
          },
          {
            text: 'Next',
            action: tour.next,
          },
        ],
      });
    });

    // ⬐ 4. Persist completion so it doesn’t annoy veterans
    tour.on('complete', () => localStorage.setItem(`tour:${pageId}`, 'done'));
    if (localStorage.getItem(`tour:${pageId}`) !== 'done') tour.start();
  }
}
