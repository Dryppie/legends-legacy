import { DOCUMENT } from '@angular/common';
import { Inject, Injectable, Injector } from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { HelpDrawerComponent } from './help-drawer.component';
import { HELP_PAGE_ID } from './help.tokens';

@Injectable({ providedIn: 'root' })
export class HelpOverlayService {
  constructor(
    private overlay: Overlay,
    private injector: Injector,
    @Inject(DOCUMENT) private document: Document,
  ) {}

  open(pageId: string) {
    const previouslyFocused = this.document.activeElement as HTMLElement | null;
    const overlayRef = this.overlay.create({
      hasBackdrop: true,
      backdropClass: 'cdk-overlay-dark-backdrop',
      scrollStrategy: this.overlay.scrollStrategies.block(),
      positionStrategy: this.overlay
        .position()
        .global()
        .left('0')
        .top('0')
        .width('min(384px, 100vw)')
        .height('100dvh'),
    });

    // close on backdrop click
    overlayRef.backdropClick().subscribe(() => overlayRef.dispose());
    overlayRef.detachments().subscribe(() => previouslyFocused?.focus());

    // pass pageId + overlayRef to the drawer
    const injector = Injector.create({
      providers: [
        { provide: OverlayRef, useValue: overlayRef },
        { provide: HELP_PAGE_ID, useValue: pageId },
      ],
      parent: this.injector,
    });

    overlayRef.attach(new ComponentPortal(HelpDrawerComponent, null, injector));
  }
}
