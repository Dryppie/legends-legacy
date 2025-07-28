import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostListener,
  Input,
  OnDestroy,
  Output,
  signal,
} from '@angular/core';
import { DropdownRegistryService } from '../../../../core/services/client-side/components/dropdown/dropdown-registry.service';

export interface DropdownSelection<T = unknown> {
  main: T; // the main item (e.g. ItemType, string, number, ...)
  sub: string | null; // chosen sub‑option or null when none
}

@Component({
  selector: 'app-dropdown',
  standalone: true,
  imports: [NgClass, NgFor, NgIf],
  templateUrl: './dropdown.component.html',
})
export class DropdownComponent<T = unknown> implements OnDestroy {
  /** The item this button represents (enum, string, number – anything). */
  @Input({ required: true }) value!: T;

  /** Text shown inside the button. */
  @Input({ required: true }) label!: string;

  /** List of sub‑options. Empty → act as a simple button. */
  @Input() subOptions: readonly string[] = [];

  /** Whether the parent considers this the active/main selection. */
  @Input() selected = false;

  @Output() readonly selection = new EventEmitter<DropdownSelection<T>>();

  readonly open = signal(false);

  constructor(private readonly registry: DropdownRegistryService) {}

  get hasSubOptions(): boolean {
    return this.subOptions.length > 0;
  }

  onButtonClick(event: MouseEvent): void {
    // NOTE: do NOT stop propagation so other dropdowns can treat this as an outside click.

    // First, close any other dropdown that might be open.
    // We always do this, even if we ourselves have no sub‑options.
    this.registry.register(this);

    if (!this.hasSubOptions) {
      // Simple button → emit immediately and clear registry (nothing remains open).
      this.registry.clear(this);
      this.selection.emit({ main: this.value, sub: null });
      return;
    }

    // Toggle our own dropdown.
    const nowOpen = !this.open();
    this.open.set(nowOpen);

    if (!nowOpen) {
      // We just closed ourselves → inform registry.
      this.registry.clear(this);
    }
  }

  onSubOptionClick(sub: string): void {
    this.close();
    this.registry.clear(this);
    this.selection.emit({ main: this.value, sub });
  }

  /** Public API required by the registry. */
  close(): void {
    this.open.set(false);
  }

  /* --------------------------------------------------------------------
   * Close on outside click.
   * ------------------------------------------------------------------ */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;

    const target = event.target as HTMLElement | null;
    if (target && target.closest('app-dropdown') === null) {
      this.close();
      this.registry.clear(this);
    }
  }

  /* --------------------------------------------------------------------
   * Lifecycle
   * ------------------------------------------------------------------ */
  ngOnDestroy(): void {
    this.registry.clear(this);
  }
}
