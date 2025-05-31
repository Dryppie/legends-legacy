import { coerceBooleanProperty } from '@angular/cdk/coercion';
import { NgClass, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-regular-button',
  standalone: true,
  imports: [NgClass, NgIf],
  templateUrl: './regular-button.component.html',
  styleUrl: './regular-button.component.css',
})
export class RegularButtonComponent {
  /** Button label. If omitted, projected content (icon, custom markup) is rendered. */
  @Input() text?: string;

  /** When true the button becomes inert/disabled and visuals signify such. */
  @Input() disabled = false;

  /** Visual palette variant – extend with more (e.g. outline, ghost, link) as needed. */
  @Input() variant: 'primary' | 'secondary' | 'danger' = 'primary';

  private _fullWidth = false;
  @Input()
  set fullWidth(value: boolean | string) {
    this._fullWidth = coerceBooleanProperty(value);
  }
  get fullWidth(): boolean {
    return this._fullWidth;
  }

  /** Notify consumers when the button is activated. */
  @Output() pressed = new EventEmitter<Event>();

  /** Derive the Tailwind class string based on selected variant & disabled state. */
  get buttonClasses(): string {
    const palette = {
      primary: 'border-light_gray text-primary hover:bg-zinc-600/30',
      secondary: 'border-light_gray text-emerald-400 hover:bg-gray-200',
      danger: 'border-light_gray text-rose-500 hover:bg-zinc-600/30',
    }[this.variant];

    const disabledStyles = 'text-zinc-300 opacity-50 hover:bg-transparent';
    const width = this.fullWidth ? 'w-full' : '';

    return `${palette} ${width} ${this.disabled ? disabledStyles : ''}`;
  }

  /** Emit `pressed` only when not disabled, preventing propagation otherwise. */
  onClick(event: Event): void {
    if (this.disabled) {
      event.preventDefault();
      event.stopImmediatePropagation();
      return;
    }

    this.pressed.emit(event);
  }
}
