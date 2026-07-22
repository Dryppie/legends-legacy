import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DropdownRegistryService {
  private current: { close: () => void } | null = null;

  register(dropdown: { close: () => void }): void {
    if (this.current && this.current !== dropdown) {
      this.current.close();
    }

    this.current = dropdown;
  }

  clear(dropdown: { close: () => void }): void {
    if (this.current === dropdown) {
      this.current = null;
    }
  }
}
