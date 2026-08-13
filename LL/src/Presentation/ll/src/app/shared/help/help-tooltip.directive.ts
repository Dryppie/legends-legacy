// shared/help/help-tooltip.directive.ts
import {
  Directive,
  Input,
  ElementRef,
  HostListener,
  inject,
  OnDestroy,
} from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import { HelpEntry, HelpService } from './help.service';
import { HelpTooltipPanelComponent } from './help-tooltip-panel.component';
import { firstValueFrom } from 'rxjs';

@Directive({
  selector: '[appHelp]',
  standalone: true,
})
export class HelpTooltipDirective implements OnDestroy {
  @Input('appHelp') helpId!: string;

  private overlayRef?: OverlayRef;
  private overlay = inject(Overlay);
  private help = inject(HelpService);
  private host = inject<ElementRef<HTMLElement>>(ElementRef);
  private pointerInside = false;
  private requestVersion = 0;
  private destroyed = false;

  @HostListener('mouseenter')
  onMouseEnter(): void {
    this.pointerInside = true;
    void this.show();
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.pointerInside = false;
    this.hide();
  }

  @HostListener('window:blur')
  onWindowBlur(): void {
    this.pointerInside = false;
    this.hide();
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.pointerInside = false;
    this.hide();
  }

  private async show(): Promise<void> {
    if (this.overlayRef) return;
    const requestVersion = ++this.requestVersion;
    let dict: Record<string, HelpEntry>;
    try {
      dict = await firstValueFrom(this.help.load('en'));
    } catch {
      return;
    }

    if (
      this.destroyed ||
      !this.pointerInside ||
      requestVersion !== this.requestVersion ||
      this.overlayRef
    ) {
      return;
    }

    const entry = dict[this.helpId];
    if (!entry) return;

    const overlayRef = this.overlay.create({
      positionStrategy: this.overlay
        .position()
        .flexibleConnectedTo(this.host)
        .withPositions([
          {
            originX: 'end',
            originY: 'center',
            overlayX: 'start',
            overlayY: 'center',
            offsetX: 10,
          },
          {
            originX: 'start',
            originY: 'center',
            overlayX: 'end',
            overlayY: 'center',
            offsetX: -10,
          },
          {
            originX: 'center',
            originY: 'top',
            overlayX: 'center',
            overlayY: 'bottom',
            offsetY: -10,
          },
        ]),
      hasBackdrop: false,
      panelClass: 'help-tooltip-panel',
      scrollStrategy: this.overlay.scrollStrategies.close(),
    });
    this.overlayRef = overlayRef;

    overlayRef.detachments().subscribe(() => {
      if (this.overlayRef !== overlayRef) return;
      this.overlayRef = undefined;
      overlayRef.dispose();
    });

    const portal = new ComponentPortal(HelpTooltipPanelComponent);
    const compRef = overlayRef.attach(portal);
    compRef.instance.entry = entry;
  }

  private hide(): void {
    this.requestVersion += 1;
    const overlayRef = this.overlayRef;
    this.overlayRef = undefined;
    overlayRef?.dispose();
  }
}
