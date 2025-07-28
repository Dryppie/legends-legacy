import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class DropdownRegistryService {
  private current: { close: () => void } | null = null;

  register(drop: { close: () => void }) {
    // Close any previously-open dropdown.
    if (this.current && this.current !== drop) {
      this.current.close();
    }
    this.current = drop;
  }

  clear(drop: { close: () => void }) {
    if (this.current === drop) this.current = null;
  }
}
