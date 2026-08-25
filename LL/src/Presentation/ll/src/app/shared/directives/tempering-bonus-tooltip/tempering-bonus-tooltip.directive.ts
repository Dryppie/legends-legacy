import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { ComponentPortal } from '@angular/cdk/portal';
import {
  Directive,
  ElementRef,
  HostBinding,
  HostListener,
  Input,
  OnDestroy,
  inject,
} from '@angular/core';
import {
  TemperingBonusTooltipData,
  TemperingBonusTooltipPanelComponent,
} from '../../components/custom-components/tooltips/tempering-bonus-tooltip/tempering-bonus-tooltip-panel.component';

let nextTemperingBonusTooltipId = 1;
const temperingBonusTooltipCloseDelayMs = 120;

@Directive({
  selector: '[appTemperingBonusTooltip]',
  standalone: true,
})
export class TemperingBonusTooltipDirective implements OnDestroy {
  @Input('appTemperingBonusTooltip') data!: TemperingBonusTooltipData;

  @HostBinding('class.cursor-help') readonly cursorClass = true;
  @HostBinding('attr.tabindex') readonly tabindex = '0';
  @HostBinding('attr.aria-describedby') describedBy: string | null = null;
  @HostBinding('attr.aria-label')
  get ariaLabel(): string | null {
    if (!this.data) return null;
    return `${this.data.attributeName} tempered by ${this.data.bonusAmount}`;
  }

  private readonly overlay = inject(Overlay);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tooltipId = `tempering-bonus-tooltip-${nextTemperingBonusTooltipId++}`;
  private overlayRef?: OverlayRef;
  private hovered = false;
  private focused = false;
  private hideTimer?: ReturnType<typeof setTimeout>;

  @HostListener('mouseenter')
  onMouseEnter(): void {
    this.hovered = true;
    this.clearHideTimer();
    this.show();
  }

  @HostListener('mouseleave')
  onMouseLeave(): void {
    this.hovered = false;
    this.hideWhenInactive();
  }

  @HostListener('focusin')
  onFocusIn(): void {
    this.focused = true;
    this.clearHideTimer();
    this.show();
  }

  @HostListener('focusout')
  onFocusOut(): void {
    this.focused = false;
    this.hideWhenInactive();
  }

  @HostListener('keydown.escape')
  onEscape(): void {
    this.hide();
  }

  ngOnDestroy(): void {
    this.clearHideTimer();
    this.hide();
  }

  private show(): void {
    if (this.overlayRef?.hasAttached() || !this.data) return;

    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.host)
      .withPositions([
        {
          originX: 'center',
          originY: 'bottom',
          overlayX: 'center',
          overlayY: 'top',
          offsetY: 8,
        },
        {
          originX: 'center',
          originY: 'top',
          overlayX: 'center',
          overlayY: 'bottom',
          offsetY: -8,
        },
      ])
      .withPush(true)
      .withViewportMargin(8);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: false,
      panelClass: ['tempering-bonus-tooltip-panel', 'pointer-events-none'],
    });

    const componentRef = this.overlayRef.attach(
      new ComponentPortal(TemperingBonusTooltipPanelComponent),
    );
    componentRef.setInput('data', this.data);
    componentRef.setInput('tooltipId', this.tooltipId);
    this.describedBy = this.tooltipId;
  }

  private hideWhenInactive(): void {
    if (this.hovered || this.focused) return;

    this.clearHideTimer();
    this.hideTimer = setTimeout(() => {
      this.hideTimer = undefined;

      if (this.host.nativeElement.matches(':hover')) {
        this.hovered = true;
        return;
      }

      if (!this.focused) this.hide();
    }, temperingBonusTooltipCloseDelayMs);
  }

  private clearHideTimer(): void {
    if (!this.hideTimer) return;
    clearTimeout(this.hideTimer);
    this.hideTimer = undefined;
  }

  private hide(): void {
    this.clearHideTimer();
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.describedBy = null;
  }
}
