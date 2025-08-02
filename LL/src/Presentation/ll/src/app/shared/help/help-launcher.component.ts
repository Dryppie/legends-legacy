import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { HelpOverlayService } from './help-overlay.service';

@Component({
  selector: 'app-help-launcher',
  standalone: true,
  template: `
    <button
      data-tour="page-helper"
      class="z-50 h-8 w-8 rounded-full border-b border-l border-primary bg-texture text-xl text-white shadow-lg transition-transform hover:scale-[1.1] hover:bg-slate-700 sm:fixed sm:bottom-6 sm:left-6 sm:h-12 sm:w-12 sm:border-b-0 sm:border-l-0 sm:border-r sm:border-t"
      (click)="open()"
    >
      ?
    </button>
  `,
})
export class HelpLauncherComponent {
  /** Override the page guide to open; otherwise uses current route. */
  @Input() pageId?: string;
  isOpen = false;

  constructor(
    private router: Router,
    private overlay: HelpOverlayService,
  ) {}

  open() {
    const id =
      this.pageId ??
      (this.router.url.split('?')[0].replace(/^\/+/, '') || 'home');

    this.overlay.open(id);
  }
}
