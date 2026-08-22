import {
  Directive,
  ElementRef,
  HostListener,
  AfterViewInit,
  OnDestroy,
} from '@angular/core';

@Directive({
  selector: '[appStickyScroll]',
  standalone: true,
})
export class StickyScrollDirective implements AfterViewInit, OnDestroy {
  private isUserScrolledUp = false;
  private isAutoScrolling = false;
  private mutationObserver?: MutationObserver;
  private resizeObserver?: ResizeObserver;

  constructor(private el: ElementRef) {}

  ngAfterViewInit() {
    this.scrollToBottom();

    // Observe changes in the content of the element
    this.mutationObserver = new MutationObserver(() => {
      if (!this.isUserScrolledUp) {
        this.scrollToBottom();
      }
    });

    this.mutationObserver.observe(this.el.nativeElement, {
      childList: true,
      subtree: true,
    });

    // Keep the newest message visible when another panel changes the amount of
    // space available to the scroll container (for example, a growing loot
    // history). ResizeObserver does not report content mutations reliably when
    // only the container's viewport height changes.
    this.resizeObserver = new ResizeObserver(() => {
      if (!this.isUserScrolledUp) {
        this.scrollToBottom();
      }
    });
    this.resizeObserver.observe(this.el.nativeElement);
  }

  @HostListener('scroll')
  onScroll() {
    if (this.isAutoScrolling) {
      this.isAutoScrolling = false;
      return;
    }
    const { scrollTop, scrollHeight, clientHeight } = this.el.nativeElement;
    this.isUserScrolledUp = scrollTop + clientHeight < scrollHeight - 1;
  }

  private scrollToBottom() {
    // Ensure the scroll happens after the content is rendered
    requestAnimationFrame(() => {
      this.isAutoScrolling = true;
      this.el.nativeElement.scrollTop = this.el.nativeElement.scrollHeight;
    });
  }

  ngOnDestroy() {
    this.mutationObserver?.disconnect();
    this.resizeObserver?.disconnect();
  }
}
