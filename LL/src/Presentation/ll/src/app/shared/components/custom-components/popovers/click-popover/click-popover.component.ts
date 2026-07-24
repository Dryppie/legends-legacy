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
    selector: 'app-click-popover',
    imports: [],
    templateUrl: './click-popover.component.html'
})
export class ClickPopoverComponent implements AfterViewInit, OnDestroy {
  /** Template to render inside the popover */
  @Input({ required: true }) template!: TemplateRef<any>;

  /** CSS class(es) applied to the popover root element */
  @Input() popoverClass =
    'bg-texture border border-light_gray rounded shadow p-2 text-sm text-white';

  /** Element that triggers the pop‑over */
  @ViewChild('trigger', { static: true }) triggerEl!: ElementRef<HTMLElement>;

  private popoverEl?: HTMLElement;
  private view?: EmbeddedViewRef<any>;
  private outsideClickListener = this.handleOutsideClick.bind(this);

  constructor(
    private vcr: ViewContainerRef,
    private cdr: ChangeDetectorRef,
  ) {}

  ngAfterViewInit(): void {
    this.triggerEl.nativeElement.addEventListener('click', (e) => {
      e.stopPropagation();
      this.togglePopover();
    });
  }

  ngOnDestroy(): void {
    this.destroyPopover();
  }

  /** Toggle popover visibility */
  togglePopover(): void {
    if (this.popoverEl) {
      this.destroyPopover();
    } else {
      this.createPopover();
    }
  }

  private createPopover(): void {
    // Create container
    const container = document.createElement('div');
    container.style.position = 'fixed';
    container.style.zIndex = '50';
    container.className = this.popoverClass;
    document.body.appendChild(container);
    this.popoverEl = container;

    // Insert angular template
    this.view = this.vcr.createEmbeddedView(this.template);
    this.view.detectChanges();
    this.view.rootNodes.forEach((n) => container.appendChild(n));

    // Position after next change detection so we know size
    setTimeout(() => this.positionPopover(), 0);

    // Listen for outside clicks to close
    document.addEventListener('click', this.outsideClickListener, true);
  }

  private destroyPopover(): void {
    if (this.view) {
      this.view.destroy();
      this.view = undefined;
    }
    if (this.popoverEl) {
      this.popoverEl.remove();
      this.popoverEl = undefined;
    }
    document.removeEventListener('click', this.outsideClickListener, true);
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
      // Prefer placing above
      top = triggerRect.top - popRect.height - 4;
    } else {
      // Fallback to placing below
      top = triggerRect.bottom + 4;
    }

    // Ensure within horizontal viewport
    if (left + popRect.width > window.innerWidth) {
      left = window.innerWidth - popRect.width - 4;
    }
    if (left < 4) left = 4;

    this.popoverEl.style.top = `${Math.max(top, 4)}px`;
    this.popoverEl.style.left = `${left}px`;
  }

  private handleOutsideClick(event: MouseEvent): void {
    const target = event.target as Node;
    if (
      this.popoverEl &&
      !this.popoverEl.contains(target) &&
      !this.triggerEl.nativeElement.contains(target)
    ) {
      this.destroyPopover();
    }
  }
}
