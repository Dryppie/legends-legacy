import { Component, Input } from '@angular/core';
import { Router } from '@angular/router';
import { HelpOverlayService } from './help-overlay.service';

@Component({
  selector: 'app-help-launcher',
  standalone: true,
  template: `
    <button
      class="z-50 h-12 w-12 rounded-full border-b border-l border-primary bg-texture text-xl text-white shadow-lg hover:bg-slate-700 focus:outline-none sm:fixed sm:bottom-6 sm:left-6 sm:border-b-0 sm:border-l-0 sm:border-r sm:border-t"
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
