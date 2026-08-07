import { NgIf } from '@angular/common';
import { Component, inject, Input } from '@angular/core';
import { Router } from '@angular/router';
import { HelpOverlayService } from './help-overlay.service';

@Component({
  selector: 'app-help-launcher',
  imports: [NgIf],
  template: `
    <button
      *ngIf="presentation === 'floating'; else inlineButton"
      type="button"
      data-tour="page-helper"
      class="z-50 h-8 w-8 rounded-full border-b border-l border-primary bg-texture text-xl text-white shadow-lg transition-transform hover:scale-[1.1] hover:bg-slate-700 sm:fixed sm:bottom-6 sm:left-6 sm:h-12 sm:w-12 sm:border-b-0 sm:border-l-0 sm:border-r sm:border-t"
      (click)="open()"
      [attr.aria-label]="ariaLabel"
    >
      {{ label }}
    </button>
    <ng-template #inlineButton>
      <span
        class="inline-flex h-10 w-10 items-center justify-center sm:h-14 sm:w-14"
      >
        <button
          type="button"
          data-tour="page-helper"
          class="z-50 h-8 w-8 rounded-full border-b border-l border-primary bg-texture text-xl text-white shadow-lg transition-transform hover:scale-[1.1] hover:bg-slate-700 sm:h-12 sm:w-12 sm:border-b-0 sm:border-l-0 sm:border-r sm:border-t"
          (click)="open()"
          [attr.aria-label]="ariaLabel"
        >
          ?
        </button>
      </span>
    </ng-template>
  `,
})
export class HelpLauncherComponent {
  private readonly router = inject(Router, { optional: true });
  private readonly overlay = inject(HelpOverlayService);

  /** Override the page guide to open; otherwise uses current route. */
  @Input() pageId?: string;
  @Input() presentation: 'floating' | 'inline' = 'floating';
  @Input() label = '?';
  @Input() ariaLabel = 'Open help';

  open() {
    const id = this.pageId ?? this.routeGuidePageId() ?? this.routePath();

    this.overlay.open(id);
  }

  private routeGuidePageId(): string | undefined {
    let route = this.router?.routerState.snapshot.root;
    while (route?.firstChild) route = route.firstChild;

    const id = route?.data?.['guidePageId'];
    return typeof id === 'string' && id ? id : undefined;
  }

  private routePath(): string {
    return this.router?.url.split(/[?#]/, 1)[0].replace(/^\/+/, '') || 'home';
  }
}
