import { Injectable } from '@angular/core';
import { Router, NavigationStart } from '@angular/router';

export type TriggerType = 'click' | 'hover';

export interface PopoverHandle {
  id: string; // per instance (e.g., uniqueId)
  trigger: TriggerType;
  isOpen: () => boolean;
  open: () => void;
  close: () => void;
}

@Injectable({ providedIn: 'root' })
export class PopoverService {
  /** enforce single open hover popover at a time */
  private currentHover?: PopoverHandle;
  /** optional: also serialize clicks globally (toggle if you want exclusivity) */
  private currentClick?: PopoverHandle;

  constructor(router: Router) {
    // Close all on route change
    router.events.subscribe((e) => {
      if (e instanceof NavigationStart) {
        this.currentHover?.close();
        this.currentClick?.close();
        this.currentHover = undefined;
        this.currentClick = undefined;
      }
    });
  }

  register(handle: PopoverHandle) {
    // no-op here; kept for symmetry if you later want a registry
    return {
      requestOpen: () => this.requestOpen(handle),
      requestClose: () => this.requestClose(handle),
      requestToggle: () => this.requestToggle(handle),
      forceCloseAll: () => this.forceCloseAll(),
    };
  }

  private requestOpen(handle: PopoverHandle) {
    if (handle.trigger === 'hover') {
      if (this.currentHover && this.currentHover.id !== handle.id) {
        this.currentHover.close();
      }
      this.currentHover = handle;
      if (!handle.isOpen()) handle.open();
    } else {
      // decide if you want mutual exclusivity for clicks as well
      if (this.currentClick && this.currentClick.id !== handle.id) {
        this.currentClick.close();
      }
      this.currentClick = handle;
      if (!handle.isOpen()) handle.open();
    }
  }

  private requestClose(handle: PopoverHandle) {
    handle.close();
    if (this.currentHover?.id === handle.id) this.currentHover = undefined;
    if (this.currentClick?.id === handle.id) this.currentClick = undefined;
  }

  private requestToggle(handle: PopoverHandle) {
    if (handle.isOpen()) this.requestClose(handle);
    else this.requestOpen(handle);
  }

  private forceCloseAll() {
    this.currentHover?.close();
    this.currentClick?.close();
    this.currentHover = this.currentClick = undefined;
  }
}
