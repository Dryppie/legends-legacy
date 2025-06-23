// shared/help/help-tooltip.directive.ts
import {
  Directive,
  Input,
  ElementRef,
  HostListener,
  inject,
} from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { HelpService } from './help.service';
import { HelpTooltipPanelComponent } from './help-tooltip-panel.component';

@Directive({
  selector: '[appHelp]',
  standalone: true,
})
export class HelpTooltipDirective {
  @Input('appHelp') helpId!: string;

  private overlayRef?: OverlayRef;
  private overlay = inject(Overlay);
  private help = inject(HelpService);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);

  @HostListener('mouseenter') async show() {
    if (this.overlayRef) return;
    const dict = this.help.load('en');
    const entry = dict[this.helpId];
    if (!entry) return;

    this.overlayRef = this.overlay.create({
      positionStrategy: this.overlay
        .position()
        .flexibleConnectedTo(this.host)
        .withPositions([
          {
            // above, centred
            originX: 'center',
            originY: 'top',
            overlayX: 'center',
            overlayY: 'bottom',
            offsetY: -8,
          },
        ]),
      hasBackdrop: false,
      panelClass: 'help-tooltip-panel',
    });

    const portal = new ComponentPortal(HelpTooltipPanelComponent);
    const compRef = this.overlayRef.attach(portal);
    compRef.instance.entry = entry;
  }

  @HostListener('mouseleave') hide() {
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
  }
}
