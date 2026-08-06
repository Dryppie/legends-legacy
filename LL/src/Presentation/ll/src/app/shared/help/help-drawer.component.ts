import {
  Component,
  inject,
  OnInit,
  HostListener,
  ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { OverlayRef } from '@angular/cdk/overlay';
import { HelpService } from './help.service';
import { HELP_PAGE_ID } from './help.tokens';
import { Observable, of } from 'rxjs';
import { catchError, map, shareReplay } from 'rxjs/operators';
import { Guide } from './help.models';
import { A11yModule } from '@angular/cdk/a11y';

@Component({
  selector: 'app-help-drawer',
  imports: [CommonModule, A11yModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  styles: [
    `
      @keyframes slide-in {
        from {
          transform: translateX(-100%);
        }
        to {
          transform: translateX(0);
        }
      }

      .help-drawer {
        animation: slide-in 180ms ease-out;
      }

      @media (prefers-reduced-motion: reduce) {
        .help-drawer {
          animation: none;
        }
      }
    `,
  ],
  template: `
    <aside
      class="help-drawer flex h-full flex-col bg-texture text-white shadow-xl sm:border-r sm:border-primary/30"
      role="dialog"
      aria-modal="true"
      aria-labelledby="help-drawer-title"
      cdkTrapFocus
      [cdkTrapFocusAutoCapture]="true"
    >
      <header
        class="flex items-center justify-between border-b border-light_gray/30 p-4"
      >
        <h2 id="help-drawer-title" class="text-lg font-semibold text-primary">
          {{ (guide$ | async)?.title }}
        </h2>
        <button
          type="button"
          (click)="close()"
          aria-label="Close guide"
          class="text-xl"
        >
          ✕
        </button>
      </header>

      <section
        class="flex-1 space-y-6 overflow-y-auto p-4"
        *ngIf="guide$ | async as guide"
      >
        <article *ngFor="let s of guide.sections">
          <h3 class="mb-1 font-medium">{{ s.heading }}</h3>
          <p [innerHTML]="s.body"></p>
        </article>
      </section>

      <footer
        *ngIf="(guide$ | async)?.lastReviewed as lastReviewed"
        class="border-t border-light_gray/30 px-4 py-3 text-xs text-light_gray"
      >
        Last reviewed {{ lastReviewed }}
      </footer>
    </aside>
  `,
})
export class HelpDrawerComponent implements OnInit {
  private pageId = inject(HELP_PAGE_ID);
  private help = inject(HelpService);
  private ovrRef = inject(OverlayRef);

  guide$!: Observable<Guide>;

  ngOnInit() {
    this.guide$ = this.help
      .load<Guide>(`assets/help/guides/${this.pageId}.json`)
      .pipe(
        map((g) => g || { title: 'Guide', sections: [] }),
        catchError(() =>
          of({
            title: 'Guide unavailable',
            lastReviewed: '',
            sections: [
              {
                heading: 'Unable to load this guide',
                body: 'Please close the guide and try again.',
              },
            ],
          }),
        ),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
  }

  @HostListener('document:keydown.escape') close() {
    this.ovrRef.dispose();
  }
}
