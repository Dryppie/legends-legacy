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
  private mutationObserver!: MutationObserver;

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
    // Disconnect the observer to prevent memory leaks
    if (this.mutationObserver) {
      this.mutationObserver.disconnect();
    }
  }
}
