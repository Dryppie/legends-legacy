import { NgFor, NgIf, NgStyle } from '@angular/common';
import { Component, HostListener, inject } from '@angular/core';
import { FirstPartyTourService } from '../../../core/services/client-side/first-party-tour/first-party-tour.service';
import {
  FirstPartyTourRect,
  FirstPartyTourSide,
  FirstPartyTourViewState,
} from '../../../core/services/client-side/first-party-tour/first-party-tour.models';

interface PopoverCoordinates {
  top: number;
  left: number;
}

@Component({
  selector: 'app-first-party-tour-overlay',
  imports: [NgFor, NgIf, NgStyle],
  template: `
    <ng-container *ngIf="state() as tour">
      <div class="first-party-tour-root" aria-live="polite">
        <div
          *ngFor="let style of backdropStyles(tour.targetRect)"
          class="first-party-tour-backdrop"
          [class.first-party-tour-backdrop-tutorial]="isTutorial(tour)"
          [ngStyle]="style"
          [style.pointer-events]="tour.blocksInteraction || isTutorial(tour) ? 'auto' : 'none'"
        ></div>

        <div
          *ngIf="tour.targetRect"
          class="first-party-tour-highlight"
          [class.first-party-tour-highlight-tutorial]="isTutorial(tour)"
          [class.first-party-tour-highlight-action-sweep]="
            isTutorial(tour) && requiresTargetAction(tour)
          "
          [ngStyle]="highlightStyle(tour.targetRect)"
          aria-hidden="true"
        ></div>

        <section
          class="ll-panel ll-panel-strong first-party-tour-popover bg-texture"
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
          <p
            *ngIf="tour.instruction"
            class="ll-state ll-state-info mt-3 text-xs"
          >
            {{ tour.instruction }}
          </p>
          <div class="mt-4 flex flex-wrap items-center justify-between gap-3">
            <span class="text-sm font-bold text-white">
              {{ tour.stepIndex + 1 }} of {{ tour.stepCount }}
            </span>

            <div class="flex min-w-0 flex-wrap items-center justify-end gap-2">
              <button
                *ngIf="showBackButton(tour)"
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

      .first-party-tour-backdrop-tutorial {
        background: rgba(0, 0, 0, 0.24);
        backdrop-filter: blur(0.6px);
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

      .first-party-tour-highlight-tutorial {
        box-shadow:
          0 0 0 1px rgba(255, 255, 255, 0.16),
          0 10px 30px rgba(249, 220, 160, 0.18);
      }

      .first-party-tour-highlight-action-sweep {
        background-image: linear-gradient(
          105deg,
          transparent 0%,
          transparent 43%,
          rgba(249, 220, 160, 0.38) 50%,
          transparent 57%,
          transparent 100%
        );
        background-position: 150% 0;
        background-size: 240% 100%;
        animation: first-party-tour-action-sweep 2.8s ease-in-out infinite;
      }

      .first-party-tour-popover {
        position: fixed;
        width: min(22rem, calc(100vw - 2rem));
        max-height: calc(100dvh - 2rem);
        overflow-y: auto;
        overscroll-behavior: contain;
        padding: var(--ll-space-4);
        background-color: var(--ll-color-bg-deep);
        box-shadow:
          0 18px 50px rgba(0, 0, 0, 0.72),
          0 0 0 1px rgba(249, 220, 160, 0.08);
        color: var(--ll-color-text);
        pointer-events: auto;
      }

      @keyframes first-party-tour-action-sweep {
        0%,
        8% {
          background-position: 150% 0;
        }

        72%,
        100% {
          background-position: -50% 0;
        }
      }

      @media (prefers-reduced-motion: reduce) {
        .first-party-tour-highlight-action-sweep {
          animation: none;
          background-image: none;
        }
      }

      @media (max-width: 39.999rem) {
        .first-party-tour-popover button {
          min-height: 2.75rem;
        }
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

  isTutorial(tour: FirstPartyTourViewState): boolean {
    return tour.pageId.startsWith('tutorial-');
  }

  requiresTargetAction(tour: FirstPartyTourViewState): boolean {
    return (
      tour.step.kind === 'click' ||
      (tour.step.kind === 'navigate' &&
        !!(tour.step.advanceOn?.selector ?? tour.step.actionSelector))
    );
  }

  closeGuidance(): void {
    this.tour.stop(false);
  }

  @HostListener('document:keydown.escape')
  handleEscape(): void {
    this.closeGuidance();
  }

  eyebrowLabel(tour: FirstPartyTourViewState): string {
    return this.isTutorial(tour) ? 'Tutorial' : 'Guide';
  }

  showBackButton(tour: FirstPartyTourViewState): boolean {
    return !this.isTutorial(tour);
  }

  backdropStyles(
    rect: FirstPartyTourRect | null,
  ): Array<Record<string, string>> {
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
    const top = Math.max(0, rect.top - padding);
    const left = Math.max(0, rect.left - padding);
    const right = Math.min(window.innerWidth, rect.right + padding);
    const bottom = Math.min(window.innerHeight, rect.bottom + padding);

    return {
      top: `${top}px`,
      left: `${left}px`,
      width: `${Math.max(0, right - left)}px`,
      height: `${Math.max(0, bottom - top)}px`,
    };
  }

  popoverStyle(tour: FirstPartyTourViewState): Record<string, string> {
    if (window.innerWidth < 640) {
      return this.mobilePopoverStyle(tour);
    }

    const width = Math.min(352, window.innerWidth - 32);
    const height = 220;
    const gap = 12;
    const margin = 16;
    const fallbackLeft = (window.innerWidth - width) / 2;
    const fallbackTop = (window.innerHeight - height) / 2;

    if (!tour.targetRect) {
      return {
        top: `${this.clamp(fallbackTop, margin, window.innerHeight - height - margin)}px`,
        left: `${this.clamp(fallbackLeft, margin, window.innerWidth - width - margin)}px`,
      };
    }

    const rect = tour.targetRect;
    const candidates = this.placementOrder(tour.step.position).map((side) =>
      this.popoverCoordinates(
        side,
        rect,
        width,
        height,
        gap,
        tour.step.alignment,
      ),
    );
    const coordinates =
      candidates.find((candidate) =>
        this.fitsViewport(candidate, width, height, margin),
      ) ?? candidates[0];

    return {
      top: `${this.clamp(coordinates.top, margin, window.innerHeight - height - margin)}px`,
      left: `${this.clamp(coordinates.left, margin, window.innerWidth - width - margin)}px`,
    };
  }

  private mobilePopoverStyle(
    tour: FirstPartyTourViewState,
  ): Record<string, string> {
    const margin = 12;
    const maxHeight = Math.max(
      180,
      Math.min(280, window.innerHeight * 0.46),
    );
    const target = this.mobilePlacementTarget(tour) ?? tour.targetRect;
    const placeAtTop =
      !!target && target.top + target.height / 2 > window.innerHeight / 2;

    return {
      left: `${margin}px`,
      right: `${margin}px`,
      width: 'auto',
      ...(placeAtTop
        ? { top: `${margin}px`, bottom: 'auto' }
        : {
            top: 'auto',
            bottom: `max(${margin}px, env(safe-area-inset-bottom))`,
          }),
      maxHeight: `${maxHeight}px`,
    };
  }

  private mobilePlacementTarget(
    tour: FirstPartyTourViewState,
  ): FirstPartyTourRect | null {
    const selector =
      tour.step.advanceOn?.selector ?? tour.step.actionSelector;
    if (!selector) return null;

    const actionable = Array.from(
      document.querySelectorAll<HTMLElement>(selector),
    ).find((element) => {
      const rect = element.getBoundingClientRect();
      return rect.width > 0 && rect.height > 0;
    });
    if (!actionable) return null;

    const rect = actionable.getBoundingClientRect();
    return {
      top: rect.top,
      right: rect.right,
      bottom: rect.bottom,
      left: rect.left,
      width: rect.width,
      height: rect.height,
    };
  }

  private placementOrder(preferred: FirstPartyTourSide): FirstPartyTourSide[] {
    switch (preferred) {
      case 'left':
        return ['left', 'right', 'bottom', 'top'];
      case 'right':
        return ['right', 'left', 'bottom', 'top'];
      case 'top':
        return ['top', 'bottom', 'right', 'left'];
      default:
        return ['bottom', 'top', 'right', 'left'];
    }
  }

  private popoverCoordinates(
    side: FirstPartyTourSide,
    rect: FirstPartyTourRect,
    width: number,
    height: number,
    gap: number,
    alignment: FirstPartyTourViewState['step']['alignment'],
  ): PopoverCoordinates {
    if (side === 'top') {
      return {
        top: rect.top - height - gap,
        left: this.alignedLeft(rect, width, alignment),
      };
    }

    if (side === 'left') {
      return {
        top: rect.top + rect.height / 2 - height / 2,
        left: rect.left - width - gap,
      };
    }

    if (side === 'right') {
      return {
        top: rect.top + rect.height / 2 - height / 2,
        left: rect.right + gap,
      };
    }

    return {
      top: rect.bottom + gap,
      left: this.alignedLeft(rect, width, alignment),
    };
  }

  private fitsViewport(
    coordinates: PopoverCoordinates,
    width: number,
    height: number,
    margin: number,
  ): boolean {
    return (
      coordinates.top >= margin &&
      coordinates.left >= margin &&
      coordinates.top + height <= window.innerHeight - margin &&
      coordinates.left + width <= window.innerWidth - margin
    );
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
