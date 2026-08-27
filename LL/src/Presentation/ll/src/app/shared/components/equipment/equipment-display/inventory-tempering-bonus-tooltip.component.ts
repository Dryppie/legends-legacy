import {
  CdkOverlayOrigin,
  ConnectedPosition,
  OverlayModule,
} from '@angular/cdk/overlay';
import { Component, HostBinding, Input, inject } from '@angular/core';
import {
  TemperingBonusTooltipData,
  TemperingBonusTooltipPanelComponent,
} from '../../custom-components/tooltips/tempering-bonus-tooltip/tempering-bonus-tooltip-panel.component';

let nextInventoryTemperingTooltipId = 1;

@Component({
  selector: '[appInventoryTemperingBonusTooltip]',
  standalone: true,
  imports: [OverlayModule, TemperingBonusTooltipPanelComponent],
  hostDirectives: [CdkOverlayOrigin],
  host: {
    '(mouseenter)': 'onMouseEnter()',
    '(mouseleave)': 'onMouseLeave()',
    '(focusin)': 'onFocusIn()',
    '(focusout)': 'onFocusOut()',
    '(keydown.escape)': 'close()',
  },
  template: `
    <ng-content />

    <ng-template
      cdkConnectedOverlay
      [cdkConnectedOverlayOrigin]="overlayOrigin"
      [cdkConnectedOverlayOpen]="isOpen"
      [cdkConnectedOverlayPositions]="positions"
      [cdkConnectedOverlayPush]="true"
      [cdkConnectedOverlayViewportMargin]="8"
      [cdkConnectedOverlayHasBackdrop]="false"
      [cdkConnectedOverlayPanelClass]="panelClass"
      (detach)="isOpen = false"
    >
      @if (data; as tooltipData) {
        <app-tempering-bonus-tooltip-panel
          [data]="tooltipData"
          [tooltipId]="tooltipId"
        />
      }
    </ng-template>
  `,
})
export class InventoryTemperingBonusTooltipComponent {
  @Input('appInventoryTemperingBonusTooltip')
  data: TemperingBonusTooltipData | null = null;

  @HostBinding('class.cursor-help')
  get cursorClass(): boolean {
    return this.data !== null;
  }

  @HostBinding('attr.tabindex')
  get tabindex(): string | null {
    return this.data ? '0' : null;
  }

  @HostBinding('attr.aria-label')
  get ariaLabel(): string | null {
    if (!this.data) return null;
    return `${this.data.attributeName} tempered by ${this.data.bonusAmount}`;
  }

  @HostBinding('attr.aria-describedby')
  get describedBy(): string | null {
    return this.data && this.isOpen ? this.tooltipId : null;
  }

  protected readonly overlayOrigin = inject(CdkOverlayOrigin);
  protected readonly tooltipId = `inventory-tempering-tooltip-${nextInventoryTemperingTooltipId++}`;
  protected readonly panelClass = [
    'tempering-bonus-tooltip-panel',
    'pointer-events-none',
  ];
  protected readonly positions: ConnectedPosition[] = [
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
  ];
  protected isOpen = false;

  private hovered = false;
  private focused = false;

  protected onMouseEnter(): void {
    if (!this.data) return;
    this.hovered = true;
    this.isOpen = true;
  }

  protected onMouseLeave(): void {
    this.hovered = false;
    this.closeWhenInactive();
  }

  protected onFocusIn(): void {
    if (!this.data) return;
    this.focused = true;
    this.isOpen = true;
  }

  protected onFocusOut(): void {
    this.focused = false;
    this.closeWhenInactive();
  }

  protected close(): void {
    this.isOpen = false;
  }

  private closeWhenInactive(): void {
    if (!this.hovered && !this.focused) this.close();
  }
}
