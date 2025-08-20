import {
  ConnectedPosition,
  FlexibleConnectedPositionStrategy,
  Overlay,
  OverlayConfig,
  OverlayModule,
  OverlayRef,
  ScrollStrategy,
  ScrollStrategyOptions,
} from '@angular/cdk/overlay';
import { PortalModule, TemplatePortal } from '@angular/cdk/portal';
import { CommonModule, DOCUMENT } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Inject,
  Input,
  OnDestroy,
  Output,
  TemplateRef,
  ViewChild,
  ViewContainerRef,
} from '@angular/core';
import { Subscription, fromEvent } from 'rxjs';

@Component({
  selector: 'app-popover',
  standalone: true,
  imports: [OverlayModule, PortalModule, CommonModule],
  templateUrl: './popover.component.html',
})
export class PopoverComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) template!: TemplateRef<any>;
  @Input() trigger: 'click' | 'hover' = 'click';
  @Input() popoverClass =
    'bg-texture border border-light_gray rounded shadow p-2 text-sm text-white';
  @Input() openDelay = 100;
  @Input() closeDelay = 100;
  @Input() offset = 8;
  @Input() hasArrow = true;
  @Input() closeOnEscape = true;
  @Input() closeOnOutsideClick = true;

  @Output() opened = new EventEmitter<void>();
  @Output() closed = new EventEmitter<void>();

  @ViewChild('origin', { static: true }) originRef!: ElementRef<HTMLElement>;
  @ViewChild('content', { static: true }) contentTpl!: TemplateRef<any>;

  overlayRef?: OverlayRef;
  private portal?: TemplatePortal;
  private posStrategy!: FlexibleConnectedPositionStrategy;
  private subs = new Subscription();

  // track placement for arrow orientation
  currentPlacement: 'top' | 'bottom' | 'left' | 'right' = 'bottom';

  private hoverOpenTimer?: ReturnType<typeof setTimeout>;
  private hoverCloseTimer?: ReturnType<typeof setTimeout>;

  constructor(
    private overlay: Overlay,
    private vcr: ViewContainerRef,
    private sso: ScrollStrategyOptions,
    private cdr: ChangeDetectorRef,
  ) {}

  private setPlacementAsync(p: 'top' | 'bottom' | 'left' | 'right') {
    queueMicrotask(() => {
      this.currentPlacement = p;
      this.cdr.markForCheck();
    });
  }

  ngAfterViewInit(): void {
    this.posStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.originRef)
      .withPositions(this.positions())
      .withPush(true)
      .withViewportMargin(8)
      .withDefaultOffsetY(0)
      .withGrowAfterOpen(false);

    // Update arrow direction on position change
    this.subs.add(
      this.posStrategy.positionChanges.subscribe((change) => {
        const pair = change.connectionPair;
        let next: 'top' | 'bottom' | 'left' | 'right' = 'bottom';
        if (pair.overlayY === 'bottom') next = 'top';
        else if (pair.overlayY === 'top') next = 'bottom';
        else if (pair.overlayX === 'end') next = 'left';
        else next = 'right';

        this.setPlacementAsync(next);
      }),
    );
  }

  ngOnDestroy(): void {
    this.clearTimers();
    this.detach();
    this.subs.unsubscribe();
  }

  // Trigger handlers
  onOriginClick(event: MouseEvent) {
    if (this.trigger !== 'click') return;
    event.stopPropagation();
    this.overlayRef
      ? this.detach()
      : this.attach({ useBackdrop: this.closeOnOutsideClick });
  }

  onOriginEnter() {
    if (this.trigger !== 'hover') return;
    this.clearCloseTimer();
    this.hoverOpenTimer = setTimeout(
      () => this.attach({ useBackdrop: false }),
      this.openDelay,
    );
  }

  onOriginLeave() {
    if (this.trigger !== 'hover') return;
    this.queueClose();
  }

  onPanelEnter() {
    if (this.trigger !== 'hover') return;
    this.clearCloseTimer();
  }

  onPanelLeave() {
    if (this.trigger !== 'hover') return;
    this.queueClose();
  }

  // Core attach/detach
  private attach(opts: { useBackdrop: boolean }) {
    if (this.overlayRef?.hasAttached()) return;

    const overlayConfig = this.overlay.create({
      positionStrategy: this.posStrategy.withPositions(this.positions()),
      hasBackdrop: opts.useBackdrop,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      scrollStrategy: this.sso.reposition(),
      panelClass: 'app-popover-panel', // for arrow positioning, etc.
    });

    this.overlayRef = overlayConfig;

    if (opts.useBackdrop && this.closeOnOutsideClick) {
      this.subs.add(
        this.overlayRef.backdropClick().subscribe(() => this.detach()),
      );
    }

    if (this.closeOnEscape) {
      this.subs.add(
        this.overlayRef.keydownEvents().subscribe((evt) => {
          if ((evt as KeyboardEvent).key === 'Escape') this.detach();
        }),
      );
    }

    this.portal = new TemplatePortal(this.contentTpl, this.vcr);
    this.overlayRef.attach(this.portal);
    this.opened.emit();
  }

  private detach() {
    if (!this.overlayRef) return;
    this.overlayRef.detach();
    this.overlayRef.dispose();
    this.overlayRef = undefined;
    this.portal = undefined;
    this.closed.emit();
  }

  private queueClose() {
    this.clearCloseTimer();
    this.hoverCloseTimer = setTimeout(() => this.detach(), this.closeDelay);
  }

  private clearCloseTimer() {
    if (this.hoverCloseTimer) {
      clearTimeout(this.hoverCloseTimer);
      this.hoverCloseTimer = undefined;
    }
  }

  private clearTimers() {
    if (this.hoverOpenTimer) clearTimeout(this.hoverOpenTimer);
    if (this.hoverCloseTimer) clearTimeout(this.hoverCloseTimer);
    this.hoverOpenTimer = this.hoverCloseTimer = undefined;
  }

  // Preferred placements with flipping
  private positions(): ConnectedPosition[] {
    const o = this.offset;
    return [
      // Below (preferred)
      {
        originX: 'start',
        originY: 'bottom',
        overlayX: 'start',
        overlayY: 'top',
        offsetY: o,
      },
      {
        originX: 'end',
        originY: 'bottom',
        overlayX: 'end',
        overlayY: 'top',
        offsetY: o,
      },

      // Above
      {
        originX: 'start',
        originY: 'top',
        overlayX: 'start',
        overlayY: 'bottom',
        offsetY: -o,
      },
      {
        originX: 'end',
        originY: 'top',
        overlayX: 'end',
        overlayY: 'bottom',
        offsetY: -o,
      },

      // Right
      {
        originX: 'end',
        originY: 'center',
        overlayX: 'start',
        overlayY: 'center',
        offsetX: o,
      },

      // Left
      {
        originX: 'start',
        originY: 'center',
        overlayX: 'end',
        overlayY: 'center',
        offsetX: -o,
      },
    ];
  }
}
