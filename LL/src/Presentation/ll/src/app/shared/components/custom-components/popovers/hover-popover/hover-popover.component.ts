import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EmbeddedViewRef,
  Input,
  OnDestroy,
  TemplateRef,
  ViewChild,
  ViewContainerRef,
} from '@angular/core';

@Component({
  selector: 'app-hover-popover',
  standalone: true,
  imports: [],
  templateUrl: './hover-popover.component.html',
})
export class HoverPopoverComponent implements AfterViewInit, OnDestroy {
  /** Template to render inside the popover */
  @Input({ required: true }) template!: TemplateRef<any>;

  /** CSS class(es) applied to the popover root element */
  @Input() popoverClass =
    'bg-texture border border-light_gray rounded shadow p-2 text-sm text-white sm:w-56';

  /** Trigger element */
  @ViewChild('trigger', { static: true }) triggerEl!: ElementRef<HTMLElement>;

  private popoverEl?: HTMLElement;
  private view?: EmbeddedViewRef<any>;
  private hideTimeout?: any;

  constructor(
    private vcr: ViewContainerRef,
    private cdr: ChangeDetectorRef,
  ) {}

  ngAfterViewInit(): void {
    const trigger = this.triggerEl.nativeElement;
    trigger.addEventListener('mouseenter', this.onMouseEnter);
    trigger.addEventListener('mouseleave', this.onMouseLeave);
  }

  ngOnDestroy(): void {
    this.destroyPopover();
    const trigger = this.triggerEl.nativeElement;
    trigger.removeEventListener('mouseenter', this.onMouseEnter);
    trigger.removeEventListener('mouseleave', this.onMouseLeave);
  }

  private onMouseEnter = () => {
    clearTimeout(this.hideTimeout);
    this.hideTimeout = setTimeout(() => {
      if (!this.popoverEl) {
        this.createPopover();
      }
    }, 100);
  };

  private onMouseLeave = () => {
    // Small delay so you can move cursor into the popover
    this.hideTimeout = setTimeout(() => this.destroyPopover(), 100);
  };

  private createPopover(): void {
    const container = document.createElement('div');
    container.style.position = 'fixed';
    container.style.zIndex = '50';
    container.className = this.popoverClass;
    document.body.appendChild(container);

    this.popoverEl = container;

    // Render Angular template
    this.view = this.vcr.createEmbeddedView(this.template);
    this.view.detectChanges();
    this.view.rootNodes.forEach((n) => container.appendChild(n));

    // Position once we know dimensions
    setTimeout(() => this.positionPopover(), 0);

    // Allow staying hovered
    container.addEventListener('mouseenter', this.onMouseEnter);
    container.addEventListener('mouseleave', this.onMouseLeave);
  }

  private destroyPopover(): void {
    if (this.view) {
      this.view.destroy();
      this.view = undefined;
    }
    if (this.popoverEl) {
      this.popoverEl.removeEventListener('mouseenter', this.onMouseEnter);
      this.popoverEl.removeEventListener('mouseleave', this.onMouseLeave);
      this.popoverEl.remove();
      this.popoverEl = undefined;
    }
  }

  private positionPopover(): void {
    if (!this.popoverEl) return;

    const triggerRect = this.triggerEl.nativeElement.getBoundingClientRect();
    const popRect = this.popoverEl.getBoundingClientRect();

    const spaceBelow = window.innerHeight - triggerRect.bottom;
    const spaceAbove = triggerRect.top;

    let top: number;
    let left: number = triggerRect.left;

    if (spaceAbove >= popRect.height || spaceAbove >= spaceBelow) {
      top = triggerRect.top - popRect.height - 4;
    } else {
      top = triggerRect.bottom + 4;
    }

    if (left + popRect.width > window.innerWidth) {
      left = window.innerWidth - popRect.width - 4;
    }
    if (left < 4) left = 4;

    this.popoverEl.style.top = `${Math.max(top, 4)}px`;
    this.popoverEl.style.left = `${left}px`;
  }
}
