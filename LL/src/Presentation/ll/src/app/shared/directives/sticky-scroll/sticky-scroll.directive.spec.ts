import { ElementRef } from '@angular/core';
import { StickyScrollDirective } from './sticky-scroll.directive';

describe('StickyScrollDirective', () => {
  let resizeCallback: ResizeObserverCallback;
  let mutationDisconnect: jasmine.Spy;
  let resizeDisconnect: jasmine.Spy;
  let originalMutationObserver: typeof MutationObserver;
  let originalResizeObserver: typeof ResizeObserver;

  beforeEach(() => {
    mutationDisconnect = jasmine.createSpy('mutationDisconnect');
    resizeDisconnect = jasmine.createSpy('resizeDisconnect');
    originalMutationObserver = globalThis.MutationObserver;
    originalResizeObserver = globalThis.ResizeObserver;

    class MutationObserverStub implements MutationObserver {
      constructor(_callback: MutationCallback) {}

      observe(_target: Node, _options?: MutationObserverInit): void {}
      disconnect(): void {
        mutationDisconnect();
      }
      takeRecords(): MutationRecord[] {
        return [];
      }
    }

    class ResizeObserverStub implements ResizeObserver {
      constructor(callback: ResizeObserverCallback) {
        resizeCallback = callback;
      }

      observe(_target: Element, _options?: ResizeObserverOptions): void {}
      disconnect(): void {
        resizeDisconnect();
      }
      unobserve(_target: Element): void {}
    }

    spyOn(globalThis, 'requestAnimationFrame').and.callFake((callback) => {
      callback(0);
      return 1;
    });
    Object.defineProperty(globalThis, 'MutationObserver', {
      configurable: true,
      value: MutationObserverStub,
    });
    Object.defineProperty(globalThis, 'ResizeObserver', {
      configurable: true,
      value: ResizeObserverStub,
    });
  });

  afterEach(() => {
    Object.defineProperty(globalThis, 'MutationObserver', {
      configurable: true,
      value: originalMutationObserver,
    });
    Object.defineProperty(globalThis, 'ResizeObserver', {
      configurable: true,
      value: originalResizeObserver,
    });
  });

  it('keeps the view pinned to the bottom when its height changes', () => {
    const element = scrollElement({
      scrollTop: 90,
      scrollHeight: 100,
      clientHeight: 10,
    });
    const directive = new StickyScrollDirective(new ElementRef(element));

    directive.ngAfterViewInit();
    directive.onScroll(); // consume the scroll event caused by initial positioning

    element.scrollTop = 90;
    setReadonlyMetric(element, 'clientHeight', 5);
    resizeCallback([], {} as ResizeObserver);

    expect(element.scrollTop).toBe(100);
  });

  it('does not move the view on resize after the user scrolls up', () => {
    const element = scrollElement({
      scrollTop: 90,
      scrollHeight: 100,
      clientHeight: 10,
    });
    const directive = new StickyScrollDirective(new ElementRef(element));

    directive.ngAfterViewInit();
    directive.onScroll(); // consume the scroll event caused by initial positioning
    element.scrollTop = 30;
    directive.onScroll();

    setReadonlyMetric(element, 'clientHeight', 5);
    resizeCallback([], {} as ResizeObserver);

    expect(element.scrollTop).toBe(30);
  });

  it('disconnects both observers when destroyed', () => {
    const directive = new StickyScrollDirective(
      new ElementRef(
        scrollElement({ scrollTop: 0, scrollHeight: 0, clientHeight: 0 }),
      ),
    );

    directive.ngAfterViewInit();
    directive.ngOnDestroy();

    expect(mutationDisconnect).toHaveBeenCalled();
    expect(resizeDisconnect).toHaveBeenCalled();
  });

  function scrollElement(metrics: {
    scrollTop: number;
    scrollHeight: number;
    clientHeight: number;
  }): HTMLElement {
    const element = { scrollTop: metrics.scrollTop } as HTMLElement;
    setReadonlyMetric(element, 'scrollHeight', metrics.scrollHeight);
    setReadonlyMetric(element, 'clientHeight', metrics.clientHeight);
    return element;
  }

  function setReadonlyMetric(
    element: HTMLElement,
    property: 'scrollHeight' | 'clientHeight',
    value: number,
  ): void {
    Object.defineProperty(element, property, { configurable: true, value });
  }
});
