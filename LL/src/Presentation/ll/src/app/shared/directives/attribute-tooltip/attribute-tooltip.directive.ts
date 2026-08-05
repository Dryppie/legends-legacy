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
  AttributeTooltipData,
  AttributeTooltipPanelComponent,
} from '../../components/custom-components/tooltips/attribute-tooltip/attribute-tooltip-panel.component';

let nextAttributeTooltipId = 1;

@Directive({
  selector: '[appAttributeTooltip]',
  standalone: true,
})
export class AttributeTooltipDirective implements OnDestroy {
  @Input('appAttributeTooltip') data!: AttributeTooltipData;
  @Input() attributeTooltipPosition: 'side' | 'below' = 'side';

  @HostBinding('class.cursor-help') readonly cursorClass = true;
  @HostBinding('attr.tabindex') readonly tabindex = '0';
  @HostBinding('attr.aria-describedby') describedBy: string | null = null;

  private readonly overlay = inject(Overlay);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly tooltipId = `attribute-tooltip-${nextAttributeTooltipId++}`;
  private overlayRef?: OverlayRef;
  private hovered = false;
  private focused = false;

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
    if (this.overlayRef?.hasAttached() || !this.data) return;

    const positionStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.host)
      .withPositions(this.positions())
      .withPush(true)
      .withViewportMargin(8);

    this.overlayRef = this.overlay.create({
      positionStrategy,
      scrollStrategy: this.overlay.scrollStrategies.reposition(),
      hasBackdrop: false,
      panelClass: 'attribute-tooltip-panel',
    });

    const componentRef = this.overlayRef.attach(
      new ComponentPortal(AttributeTooltipPanelComponent),
    );
    componentRef.setInput('data', this.data);
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

  private positions() {
    const below = {
      originX: 'center' as const,
      originY: 'bottom' as const,
      overlayX: 'center' as const,
      overlayY: 'top' as const,
      offsetY: 8,
    };
    const above = {
      originX: 'center' as const,
      originY: 'top' as const,
      overlayX: 'center' as const,
      overlayY: 'bottom' as const,
      offsetY: -8,
    };
    const right = {
      originX: 'end' as const,
      originY: 'center' as const,
      overlayX: 'start' as const,
      overlayY: 'center' as const,
      offsetX: 8,
    };
    const left = {
      originX: 'start' as const,
      originY: 'center' as const,
      overlayX: 'end' as const,
      overlayY: 'center' as const,
      offsetX: -8,
    };

    return this.attributeTooltipPosition === 'below'
      ? [below, above, right, left]
      : [right, left, above, below];
  }
}
