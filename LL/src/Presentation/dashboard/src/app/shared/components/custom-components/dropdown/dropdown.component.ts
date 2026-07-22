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
  main: T;
  sub: string | null;
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
  host: { class: 'relative inline-block' },
})
export class DropdownComponent<T = unknown> implements OnDestroy {
  private static nextId = 0;

  @Input({ required: true }) value!: T;
  @Input({ required: true }) label!: string;
  @Input() options: readonly DropdownOption<T>[] = [];
  @Input() selectedValue: T | null = null;
  @Input() appearance: 'button' | 'field' = 'button';
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

  get hasOptions(): boolean {
    return this.options.length > 0;
  }

  get displayLabel(): string {
    return (
      this.options.find((option) => option.value === this.selectedValue)
        ?.label ?? this.label
    );
  }

  get buttonClasses(): string {
    const classes = [
      this.appearance === 'field'
        ? 'll-select h-9 hover:border-primary/70 hover:bg-primary/10'
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

  onButtonClick(): void {
    this.registry.register(this);
    const nowOpen = !this.open();
    this.open.set(nowOpen);

    if (!nowOpen) {
      this.registry.clear(this);
    }
  }

  onOptionPointerDown(event: PointerEvent, option: DropdownOption<T>): void {
    event.preventDefault();
    event.stopPropagation();
    this.selectOption(option);
  }

  onOptionClick(option: DropdownOption<T>): void {
    this.selectOption(option);
  }

  onOptionKeydown(event: KeyboardEvent, option: DropdownOption<T>): void {
    if (event.key !== 'Enter' && event.key !== ' ') return;

    event.preventDefault();
    this.selectOption(option);
  }

  onDropdownKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Escape' || !this.open()) return;

    event.preventDefault();
    this.close();
    this.registry.clear(this);
  }

  onOptionPointerEnter(index: number): void {
    this.hoveredOptionIndex.set(index);
  }

  onOptionPointerLeave(index: number): void {
    if (this.hoveredOptionIndex() === index) {
      this.hoveredOptionIndex.set(null);
    }
  }

  isOptionHovered(index: number): boolean {
    return this.hoveredOptionIndex() === index;
  }

  isOptionSelected(option: DropdownOption<T>): boolean {
    return option.value === this.selectedValue;
  }

  close(): void {
    this.open.set(false);
    this.hoveredOptionIndex.set(null);
  }

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

  ngOnDestroy(): void {
    this.registry.clear(this);
  }

  private selectOption(option: DropdownOption<T>): void {
    if (option.disabled) return;

    this.close();
    this.registry.clear(this);
    this.selection.emit({ main: option.value, sub: null });
  }
}
