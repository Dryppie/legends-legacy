import {
  ConnectedPosition,
  FlexibleConnectedPositionStrategy,
  Overlay,
  OverlayModule,
  OverlayRef,
  ScrollStrategyOptions,
} from '@angular/cdk/overlay';
import { PortalModule, TemplatePortal } from '@angular/cdk/portal';
import { CommonModule } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  TemplateRef,
  ViewChild,
  ViewContainerRef,
} from '@angular/core';
import { BehaviorSubject, Subscription } from 'rxjs';
import {
  PopoverService,
  TriggerType,
} from '../../../../core/services/client-side/components/popover/popover.service';

let _nextId = 1;

@Component({
  selector: 'app-popover',
  imports: [OverlayModule, PortalModule, CommonModule],
  templateUrl: './popover.component.html',
})
export class PopoverComponent implements AfterViewInit, OnDestroy {
  @Input({ required: true }) template!: TemplateRef<any>;
  @Input() trigger: TriggerType = 'click';
  @Input() disabled = false;
  @Input() originClass = 'relative inline-block';
  @Input() popoverClass =
    'bg-texture border border-light_gray rounded shadow p-2 text-sm text-white';
  @Input() openDelay = 100; // hover only
  @Input() closeDelay = 100; // hover only
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

  // arrow placement as observable to avoid NG0100
  placement$ = new BehaviorSubject<'top' | 'bottom' | 'left' | 'right'>(
    'bottom',
  );

  private hoverOpenTimer?: ReturnType<typeof setTimeout>;
  private hoverCloseTimer?: ReturnType<typeof setTimeout>;
  private lastOriginPointerType?: string;
  private openedByTouch = false;

  // service handle
  private handleCtrl!: ReturnType<PopoverService['register']>;
  private id = `popover-${_nextId++}`;

  constructor(
    private overlay: Overlay,
    private vcr: ViewContainerRef,
    private sso: ScrollStrategyOptions,
    private popovers: PopoverService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngAfterViewInit(): void {
    this.posStrategy = this.overlay
      .position()
      .flexibleConnectedTo(this.originRef)
      .withPositions(this.positions())
      .withPush(true)
      .withViewportMargin(8);

    // Reflect actual placement into placement$ (deferred by async pipe)
    this.subs.add(
      this.posStrategy.positionChanges.subscribe((change) => {
        const pair = change.connectionPair;
        const next =
          pair.overlayY === 'bottom'
            ? 'top'
            : pair.overlayY === 'top'
              ? 'bottom'
              : pair.overlayX === 'end'
                ? 'left'
                : 'right';
        this.placement$.next(next);
      }),
    );

    // register with service
    this.handleCtrl = this.popovers.register({
      id: this.id,
      trigger: this.trigger,
      isOpen: () => !!this.overlayRef && this.overlayRef.hasAttached(),
      open: () =>
        this.attach({
          useBackdrop: this.trigger === 'click' && this.closeOnOutsideClick,
        }),
      close: () => this.detach(),
    });
  }

  ngOnDestroy(): void {
    this.clearTimers();
    this.detach();
    this.subs.unsubscribe();
  }

  // ========= Trigger handlers =========
  onOriginClick(e: MouseEvent) {
    if (this.disabled) return;

    if (this.trigger === 'click') {
      e.stopPropagation();
      this.handleCtrl.requestToggle();
      return;
    }

    const pointerType =
      (e as PointerEvent).pointerType || this.lastOriginPointerType;
    this.lastOriginPointerType = undefined;
    if (this.trigger === 'hover' && pointerType && pointerType !== 'mouse') {
      this.clearTimers();
      this.openedByTouch = true;
      this.handleCtrl.requestToggle();
    }
  }

  onOriginPointerDown(event: PointerEvent) {
    this.lastOriginPointerType = event.pointerType;
  }

  onOriginEnter(event: PointerEvent) {
    if (
      this.disabled ||
      this.trigger !== 'hover' ||
      event.pointerType !== 'mouse'
    ) {
      return;
    }
    this.openedByTouch = false;
    this.clearCloseTimer();
    this.hoverOpenTimer = setTimeout(
      () => this.handleCtrl.requestOpen(),
      this.openDelay,
    );
  }

  onOriginLeave(event: PointerEvent) {
    if (this.trigger !== 'hover' || event.pointerType !== 'mouse') return;
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

  // ========= Core overlay wiring =========
  private attach(opts: { useBackdrop: boolean }) {
    if (this.overlayRef?.hasAttached()) return;

    const overlayRef = this.overlay.create({
      positionStrategy: this.posStrategy.withPositions(this.positions()),
      hasBackdrop: opts.useBackdrop,
      backdropClass: 'cdk-overlay-transparent-backdrop',
      scrollStrategy:
        this.trigger === 'hover'
          ? this.sso.close() // hover closes on scroll
          : this.sso.reposition(), // click repositions on scroll
      panelClass: 'app-popover-panel',
    });

    this.overlayRef = overlayRef;

    if (opts.useBackdrop && this.closeOnOutsideClick) {
      this.subs.add(overlayRef.backdropClick().subscribe(() => this.detach()));
    }

    if (this.openedByTouch && this.closeOnOutsideClick) {
      this.subs.add(
        overlayRef.outsidePointerEvents().subscribe((event) => {
          const target = event.target;
          if (
            target instanceof Node &&
            this.originRef.nativeElement.contains(target)
          ) {
            return;
          }
          this.handleCtrl.requestClose();
        }),
      );
    }

    if (this.closeOnEscape) {
      this.subs.add(
        overlayRef.keydownEvents().subscribe((evt) => {
          if ((evt as KeyboardEvent).key === 'Escape') this.detach();
        }),
      );
    }

    this.portal = new TemplatePortal(this.contentTpl, this.vcr);
    overlayRef.attach(this.portal);
    overlayRef.updatePosition();
    this.opened.emit();
    this.cdr.markForCheck();
  }

  private detach() {
    if (!this.overlayRef) return;
    try {
      if (this.overlayRef.hasAttached()) this.overlayRef.detach();
    } finally {
      this.overlayRef.dispose();
      this.overlayRef = undefined;
      this.portal = undefined;
      this.openedByTouch = false;
      this.closed.emit();
      this.cdr.markForCheck();
    }
  }

  // ========= Hover timing helpers =========
  private queueClose() {
    this.clearCloseTimer();
    this.hoverCloseTimer = setTimeout(
      () => this.handleCtrl.requestClose(),
      this.closeDelay,
    );
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

  // ========= Position preferences =========
  private positions(): ConnectedPosition[] {
    const o = this.offset;
    return [
      // Above (preferred)
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

      // Below
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
