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
  private static nextId = 1;

  /** Template to render inside the popover */
  @Input({ required: true }) template!: TemplateRef<any>;

  /** CSS class(es) applied to the popover root element */
  @Input() popoverClass =
    'bg-texture border border-light_gray rounded shadow p-2 text-sm text-white sm:w-56';

  /** Context passed to the template (e.g. { $implicit: item }) */
  @Input() templateContext: any;

  /**
   * CSS class(es) for the wrapper around the trigger content. Defaults to a
   * block wrapper; pass an inline value when the trigger sits in inline or
   * flex content so the wrapper does not disturb the surrounding layout.
   */
  @Input() triggerClass = 'relative block';

  /** When true the popover never opens (e.g. there is nothing to show) */
  @Input() disabled = false;

  /** Trigger element */
  @ViewChild('trigger', { static: true }) triggerEl!: ElementRef<HTMLElement>;

  private popoverEl?: HTMLElement;
  private view?: EmbeddedViewRef<any>;
  private hideTimeout?: any;
  readonly popoverId = `hover-popover-${HoverPopoverComponent.nextId++}`;

  constructor(
    private vcr: ViewContainerRef,
    private cdr: ChangeDetectorRef,
  ) {}

  ngAfterViewInit(): void {
    const trigger = this.triggerEl.nativeElement;
    trigger.addEventListener('mouseenter', this.onMouseEnter);
    trigger.addEventListener('mouseleave', this.onMouseLeave);
    trigger.addEventListener('focusin', this.onMouseEnter);
    trigger.addEventListener('focusout', this.onMouseLeave);
    trigger.addEventListener('keydown', this.onKeydown);
  }

  ngOnDestroy(): void {
    this.destroyPopover();
    const trigger = this.triggerEl.nativeElement;
    trigger.removeEventListener('mouseenter', this.onMouseEnter);
    trigger.removeEventListener('mouseleave', this.onMouseLeave);
    trigger.removeEventListener('focusin', this.onMouseEnter);
    trigger.removeEventListener('focusout', this.onMouseLeave);
    trigger.removeEventListener('keydown', this.onKeydown);
  }

  private onMouseEnter = () => {
    if (this.disabled) return;
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

  private onKeydown = (event: KeyboardEvent) => {
    if (event.key !== 'Escape' || !this.popoverEl) return;
    event.preventDefault();
    this.destroyPopover();
  };

  private createPopover(): void {
    const container = document.createElement('div');
    container.style.position = 'fixed';
    // Appended to <body>, so this must out-rank any surface it can be opened
    // from - including modal backdrops (--ll-z-modal).
    container.style.setProperty('z-index', 'var(--ll-z-popover-detached, 300)');
    container.className = this.popoverClass;
    container.id = this.popoverId;
    container.setAttribute('role', 'tooltip');
    document.body.appendChild(container);

    this.popoverEl = container;
    this.triggerEl.nativeElement.setAttribute(
      'aria-describedby',
      this.popoverId,
    );

    // Render Angular template
    this.view = this.vcr.createEmbeddedView(
      this.template,
      this.templateContext,
    );
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
    this.triggerEl?.nativeElement.removeAttribute('aria-describedby');
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
