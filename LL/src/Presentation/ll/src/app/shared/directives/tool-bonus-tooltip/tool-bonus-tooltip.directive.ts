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
import { ToolBonusTooltipPanelComponent } from '../../components/custom-components/tooltips/tool-bonus-tooltip/tool-bonus-tooltip-panel.component';
import { toolBonusTooltip } from '../../components/custom-components/tooltips/tool-bonus-tooltip/tool-bonus-tooltip';
import { ToolBonusType } from '../../models/item';

let nextToolBonusTooltipId = 1;

@Directive({
  selector: '[appToolBonusTooltip]',
  standalone: true,
})
export class ToolBonusTooltipDirective implements OnDestroy {
  @Input('appToolBonusTooltip') bonusType!: ToolBonusType;

  @HostBinding('class.cursor-help') get cursorClass(): boolean {
    return this.hasTooltip;
  }
  @HostBinding('attr.tabindex') get tabindex(): string | null {
    return this.hasTooltip ? '0' : null;
  }
  @HostBinding('attr.aria-describedby') describedBy: string | null = null;

  private readonly overlay = inject(Overlay);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tooltipId = `tool-bonus-tooltip-${nextToolBonusTooltipId++}`;
  private overlayRef?: OverlayRef;
  private hovered = false;
  private focused = false;

  private get hasTooltip(): boolean {
    return toolBonusTooltip(this.bonusType) !== null;
  }

  @HostListener('mouseenter')
  onMouseEnter(): void {
    this.hovered = true;
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
    this.hide();
  }

  private show(): void {
    if (this.overlayRef?.hasAttached() || !this.hasTooltip) return;

    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.host)
      .withPositions([
        {
          originX: 'end',
          originY: 'center',
          overlayX: 'start',
          overlayY: 'center',
          offsetX: 8,
        },
        {
          originX: 'start',
          originY: 'center',
          overlayX: 'end',
          overlayY: 'center',
          offsetX: -8,
        },
        {
          originX: 'center',
          originY: 'top',
          overlayX: 'center',
          overlayY: 'bottom',
          offsetY: -8,
        },
        {
          originX: 'center',
          originY: 'bottom',
          overlayX: 'center',
          overlayY: 'top',
          offsetY: 8,
        },
      ])
      .withPush(true)
      .withViewportMargin(8);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: false,
      panelClass: 'tool-bonus-tooltip-panel',
    });

    const componentRef = this.overlayRef.attach(
      new ComponentPortal(ToolBonusTooltipPanelComponent),
    );
    componentRef.setInput('bonusType', this.bonusType);
    componentRef.setInput('tooltipId', this.tooltipId);
    this.describedBy = this.tooltipId;
  }

  private hideWhenInactive(): void {
    if (!this.hovered && !this.focused) this.hide();
  }

  private hide(): void {
    this.overlayRef?.dispose();
    this.overlayRef = undefined;
    this.describedBy = null;
  }
}
