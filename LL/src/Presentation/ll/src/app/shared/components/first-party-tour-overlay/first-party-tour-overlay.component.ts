import { NgFor, NgIf, NgStyle } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import {
  FirstPartyTourRect,
  FirstPartyTourViewState,
} from '../../../core/services/client-side/first-party-tour/first-party-tour.models';

@Component({
  selector: 'app-first-party-tour-overlay',
  standalone: true,
  imports: [NgFor, NgIf, NgStyle],
  template: `
    <ng-container *ngIf="state() as tour">
      <div class="first-party-tour-root" aria-live="polite">
        <div
          *ngFor="let style of backdropStyles(tour.targetRect)"
          class="first-party-tour-backdrop"
          [ngStyle]="style"
          [style.pointer-events]="tour.blocksInteraction ? 'auto' : 'none'"
        ></div>

        <div
          *ngIf="tour.targetRect"
          class="first-party-tour-highlight"
          [ngStyle]="highlightStyle(tour.targetRect)"
          aria-hidden="true"
        ></div>

        <section
          class="ll-panel ll-panel-strong first-party-tour-popover"
          [ngStyle]="popoverStyle(tour)"
          role="dialog"
          [attr.aria-label]="tour.step.title || 'Tour step'"
        >
          <p class="ll-eyebrow">{{ eyebrowLabel(tour) }}</p>
          <h2 *ngIf="tour.step.title" class="ll-heading mt-1 text-xl">
            {{ tour.step.title }}
          </h2>
          <p class="mt-3 text-sm leading-relaxed text-white">
            {{ tour.step.description }}
          </p>
          <p *ngIf="tour.instruction" class="ll-state ll-state-info mt-3 text-xs">
            {{ tour.instruction }}
          </p>

          <div class="mt-4 flex items-center justify-between gap-3">
            <span class="text-sm font-bold text-white">
              {{ tour.stepIndex + 1 }} of {{ tour.stepCount }}
            </span>

            <div class="flex min-w-0 items-center gap-2">
              <button
                type="button"
                class="ll-button px-3 py-1.5"
                [disabled]="!tour.canGoBack"
                (click)="back()"
              >
                Back
              </button>
              <button
                *ngIf="tour.canGoNext"
                type="button"
                class="ll-button px-3 py-1.5 text-primary"
                (click)="next()"
              >
                Next
              </button>
              <button
                *ngIf="tour.canFinish"
                type="button"
                class="ll-button px-3 py-1.5 text-primary"
                (click)="finish()"
              >
                Finish
              </button>
            </div>
          </div>
        </section>
      </div>
    </ng-container>
  `,
  styles: [
    `
      .first-party-tour-root {
        position: fixed;
        inset: 0;
        z-index: 1100;
        pointer-events: none;
      }

      .first-party-tour-backdrop {
        position: fixed;
        background: rgba(0, 0, 0, 0.56);
        backdrop-filter: blur(1px);
        pointer-events: auto;
      }

      .first-party-tour-highlight {
        position: fixed;
        border: 1px solid rgba(249, 220, 160, 0.95);
        border-radius: var(--ll-radius-sm);
        box-shadow:
          0 0 0 1px rgba(255, 255, 255, 0.16),
          0 0 0 9999px rgba(0, 0, 0, 0.02),
          0 10px 30px rgba(249, 220, 160, 0.18);
        pointer-events: none;
      }

      .first-party-tour-popover {
        position: fixed;
        width: min(22rem, calc(100vw - 2rem));
        padding: var(--ll-space-4);
        color: var(--ll-color-text);
        pointer-events: auto;
      }
    `,
  ],
})
export class FirstPartyTourOverlayComponent {
  private readonly tour = inject(FirstPartyTourService);
  readonly state = this.tour.state;

  next(): void {
    this.tour.next();
  }

  back(): void {
    this.tour.back();
  }

  finish(): void {
    this.tour.finish();
  }

  eyebrowLabel(tour: FirstPartyTourViewState): string {
    return tour.pageId.startsWith('tutorial-') ? 'Tutorial' : 'Guide';
  }

  backdropStyles(rect: FirstPartyTourRect | null): Array<Record<string, string>> {
    if (!rect) {
      return [
        {
          top: '0px',
          left: '0px',
          width: '100vw',
          height: '100vh',
        },
      ];
    }

    const padding = 8;
    const top = Math.max(0, rect.top - padding);
    const left = Math.max(0, rect.left - padding);
    const right = Math.min(window.innerWidth, rect.right + padding);
    const bottom = Math.min(window.innerHeight, rect.bottom + padding);

    return [
      {
        top: '0px',
        left: '0px',
        width: '100vw',
        height: `${top}px`,
      },
      {
        top: `${bottom}px`,
        left: '0px',
        width: '100vw',
        height: `${Math.max(0, window.innerHeight - bottom)}px`,
      },
      {
        top: `${top}px`,
        left: '0px',
        width: `${left}px`,
        height: `${Math.max(0, bottom - top)}px`,
      },
      {
        top: `${top}px`,
        left: `${right}px`,
        width: `${Math.max(0, window.innerWidth - right)}px`,
        height: `${Math.max(0, bottom - top)}px`,
      },
    ];
  }

  highlightStyle(rect: FirstPartyTourRect): Record<string, string> {
    const padding = 8;
    return {
      top: `${Math.max(0, rect.top - padding)}px`,
      left: `${Math.max(0, rect.left - padding)}px`,
      width: `${rect.width + padding * 2}px`,
      height: `${rect.height + padding * 2}px`,
    };
  }

  popoverStyle(tour: FirstPartyTourViewState): Record<string, string> {
    const width = Math.min(352, window.innerWidth - 32);
    const height = 190;
    const gap = 12;
    const fallbackLeft = (window.innerWidth - width) / 2;
    const fallbackTop = (window.innerHeight - height) / 2;

    if (!tour.targetRect) {
      return {
        top: `${this.clamp(fallbackTop, 16, window.innerHeight - height - 16)}px`,
        left: `${this.clamp(fallbackLeft, 16, window.innerWidth - width - 16)}px`,
      };
    }

    const rect = tour.targetRect;
    let top = rect.bottom + gap;
    let left = this.alignedLeft(rect, width, tour.step.alignment);

    if (tour.step.position === 'top') {
      top = rect.top - height - gap;
      left = this.alignedLeft(rect, width, tour.step.alignment);
    } else if (tour.step.position === 'left') {
      top = rect.top + rect.height / 2 - height / 2;
      left = rect.left - width - gap;
    } else if (tour.step.position === 'right') {
      top = rect.top + rect.height / 2 - height / 2;
      left = rect.right + gap;
    }

    return {
      top: `${this.clamp(top, 16, window.innerHeight - height - 16)}px`,
      left: `${this.clamp(left, 16, window.innerWidth - width - 16)}px`,
    };
  }

  private alignedLeft(
    rect: FirstPartyTourRect,
    width: number,
    alignment: FirstPartyTourViewState['step']['alignment'],
  ): number {
    if (alignment === 'start') {
      return rect.left;
    }

    if (alignment === 'end') {
      return rect.right - width;
    }

    return rect.left + rect.width / 2 - width / 2;
  }

  private clamp(value: number, min: number, max: number): number {
    if (max < min) {
      return min;
    }

    return Math.min(Math.max(value, min), max);
  }
}
