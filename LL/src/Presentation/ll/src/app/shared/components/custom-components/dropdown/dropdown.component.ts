import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
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

export interface DropdownOption<T = unknown> {
  label: string;
  value: T;
  disabled?: boolean;
  detail?: string;
}

@Component({
  selector: 'app-dropdown',
  standalone: true,
  imports: [NgClass, NgFor, NgIf, OverlayModule],
  templateUrl: './dropdown.component.html',
  host: {
    class: 'relative inline-block',
  },
})
export class DropdownComponent<T = unknown> implements OnDestroy {
  private static nextId = 0;

  /** The item this button represents (enum, string, number – anything). */
  @Input({ required: true }) value!: T;

  /** Text shown inside the button. */
  @Input({ required: true }) label!: string;

  /** List of sub‑options. Empty → act as a simple button. */
  @Input() subOptions: readonly string[] = [];

  /** Optional selectable options. When provided, this behaves like a normal dropdown field. */
  @Input() options: readonly DropdownOption<T>[] = [];

  /** Currently selected option value for option-mode dropdowns. */
  @Input() selectedValue: T | null = null;

  /** Visual treatment: toolbar button by default, form field when used in panels/forms. */
  @Input() appearance: 'button' | 'field' = 'button';

  /** Whether the parent considers this the active/main selection. */
  @Input() selected = false;

  @Output() readonly selection = new EventEmitter<DropdownSelection<T>>();

  readonly open = signal(false);
  readonly hoveredOptionIndex = signal<number | null>(null);
  readonly menuId = `ll-dropdown-menu-${DropdownComponent.nextId++}`;
  readonly dropdownPositions: ConnectedPosition[] = [
    {
      originX: 'start',
      originY: 'bottom',
      overlayX: 'start',
      overlayY: 'top',
      offsetY: 4,
    },
    {
      originX: 'end',
      originY: 'bottom',
      overlayX: 'end',
      overlayY: 'top',
      offsetY: 4,
    },
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -4,
    },
    {
      originX: 'end',
      originY: 'top',
      overlayX: 'end',
      overlayY: 'bottom',
      offsetY: -4,
    },
  ];

  constructor(private readonly registry: DropdownRegistryService) {}

  get hasSubOptions(): boolean {
    return this.subOptions.length > 0;
  }

  get hasOptions(): boolean {
    return this.options.length > 0;
  }

  get displayLabel(): string {
    if (!this.hasOptions) {
      return this.label;
    }

    return (
      this.options.find((option) => option.value === this.selectedValue)
        ?.label ?? this.label
    );
  }

  get buttonClasses(): string {
    const classes = [
      this.appearance === 'field'
        ? 'h-9 rounded border border-light_gray bg-black/20 text-white shadow-[inset_0_0_0_1px_rgba(255,255,255,0.04)] hover:border-primary/70 hover:bg-primary/10 focus-visible:outline focus-visible:outline-1 focus-visible:outline-primary/80'
        : 'h-8 rounded text-neutral-300 hover:bg-zinc-600/30',
    ];

    if (this.selected) {
      classes.push(
        'bg-primary/30 text-primary shadow-[inset_0_0_0_1px_rgba(249,220,160,0.35)] hover:bg-primary/40',
      );
    }

    if (this.open() && this.appearance === 'field') {
      classes.push('border-primary bg-primary/10 text-primary');
    }

    return classes.join(' ');
  }

  onButtonClick(event: MouseEvent): void {
    // NOTE: do NOT stop propagation so other dropdowns can treat this as an outside click.

    // First, close any other dropdown that might be open.
    // We always do this, even if we ourselves have no sub‑options.
    this.registry.register(this);

    if (!this.hasSubOptions && !this.hasOptions) {
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

  onOptionClick(option: DropdownOption<T>): void {
    this.selectOption(option);
  }

  onOptionPointerDown(event: PointerEvent, option: DropdownOption<T>): void {
    event.preventDefault();
    event.stopPropagation();

    this.selectOption(option);
  }

  onOptionPointerEnter(optionIndex: number): void {
    this.hoveredOptionIndex.set(optionIndex);
  }

  onOptionPointerLeave(optionIndex: number): void {
    if (this.hoveredOptionIndex() === optionIndex) {
      this.hoveredOptionIndex.set(null);
    }
  }

  isOptionHovered(optionIndex: number): boolean {
    return this.hoveredOptionIndex() === optionIndex;
  }

  private selectOption(option: DropdownOption<T>): void {
    if (option.disabled) return;

    this.close();
    this.registry.clear(this);
    this.selection.emit({ main: option.value, sub: null });
  }

  onSubOptionClick(sub: string): void {
    this.close();
    this.registry.clear(this);
    this.selection.emit({ main: this.value, sub });
  }

  onOptionKeydown(event: KeyboardEvent, option: DropdownOption<T>): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;

    event.preventDefault();
    this.onOptionClick(option);
  }

  onSubOptionKeydown(event: KeyboardEvent, sub: string): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;

    event.preventDefault();
    this.onSubOptionClick(sub);
  }

  onDropdownKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape' || !this.open()) return;

    event.preventDefault();
    this.close();
    this.registry.clear(this);
  }

  isOptionSelected(option: DropdownOption<T>): boolean {
    return option.value === this.selectedValue;
  }

  /** Public API required by the registry. */
  close(): void {
    this.open.set(false);
    this.hoveredOptionIndex.set(null);
  }

  /* --------------------------------------------------------------------
   * Close on outside click.
   * ------------------------------------------------------------------ */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (!this.open()) return;

    const target = event.target as HTMLElement | null;
    if (
      target &&
      target.closest('app-dropdown') === null &&
      target.closest(`#${this.menuId}`) === null
    ) {
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
