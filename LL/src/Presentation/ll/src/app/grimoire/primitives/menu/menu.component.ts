import {
  ChangeDetectionStrategy,
  Component,
  Directive,
  effect,
  inject,
  input,
  model,
  output,
} from '@angular/core';
import { CdkMenu, CdkMenuItem, CdkMenuItemCheckbox, CdkMenuTrigger } from '@angular/cdk/menu';
import { LgIconName } from '../../core/grimoire-icons';
import { LgIconComponent } from '../icon/icon.component';

/**
 * A menu: actions on the thing it opened from (D-146), on the CDK's menu. Write it in an `ng-template` that a
 * `[lgMenuTrigger]` opens, or show it in place:
 *
 *   <button lgButton="quiet" [lgMenuTrigger]="actions">Actions</button>
 *   <ng-template #actions>
 *     <lg-menu label="Item actions">
 *       <button lgMenuItem (triggered)="equip()">Equip</button>
 *       <button lgMenuItem (triggered)="sell()">Sell</button>
 *     </lg-menu>
 *   </ng-template>
 *
 * Arrow keys move through the items, Home and End go to the ends, typing finds an item by its words, Enter or Space
 * chooses it, and Escape closes the menu with focus back on its trigger. Choosing an item closes the menu.
 */
@Component({
  selector: 'lg-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [CdkMenu],
  host: {
    class: 'lg-root lg-menu',
    '[attr.aria-label]': 'label() || heading() || null',
  },
  template: `@if (heading()) {<div class="lg-menu__heading" aria-hidden="true">{{ heading() }}</div>}<ng-content />`,
  styleUrl: './menu.component.css',
})
export class LgMenuComponent {
  /** Its name: what it acts on ("Ashen Blade", "Maren"). */
  readonly label = input<string>();
  /** What it acts on, shown at its head in label capitals ("Maren"); also its name when it has no `label`. */
  readonly heading = input<string>();
}

/**
 * One action in a menu: `<button lgMenuItem (triggered)="sell()">Sell</button>`. Its words are its content. A choice
 * is `(triggered)`, which the keys fire as well as a press. `disabled` keeps it in view, passed over by the keys.
 */
@Component({
  selector: 'button[lgMenuItem]',
  imports: [LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [
    { directive: CdkMenuItem, inputs: ['cdkMenuItemDisabled: disabled'], outputs: ['cdkMenuItemTriggered: triggered'] },
  ],
  host: { class: 'lg-menu__item', type: 'button' },
  template: `@if (icon(); as name) {<lg-icon [name]="name" [size]="16" />}<span class="lg-menu__label"><ng-content /></span>`,
  styleUrl: './menu-item.component.css',
})
export class LgMenuItemComponent {
  /** An icon before the words, for actions that have one. The words stay. */
  readonly icon = input<LgIconName>();
}

/**
 * A setting in a menu, ticked or not: `<button lgMenuItemCheckbox [(checked)]="showLoot">Loot</button>`. Choosing it
 * turns it over and closes the menu.
 */
@Component({
  selector: 'button[lgMenuItemCheckbox]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [{ directive: CdkMenuItemCheckbox, inputs: ['cdkMenuItemDisabled: disabled'] }],
  host: { class: 'lg-menu__item lg-menu__item--check', type: 'button', '[class.is-checked]': 'checked()' },
  template: `<span class="lg-menu__check" aria-hidden="true"></span><span class="lg-menu__label"><ng-content /></span>`,
  styleUrl: './menu-item.component.css',
})
export class LgMenuItemCheckboxComponent {
  readonly checked = model(false);
  /** It was chosen: `checked` has turned over. */
  readonly triggered = output<boolean>();

  constructor() {
    const item = inject(CdkMenuItemCheckbox);
    effect(() => (item.checked = this.checked()));
    // The CDK turns its own state over after it says it was chosen; this follows it.
    item.triggered.subscribe(() => {
      const next = !this.checked();
      this.checked.set(next);
      this.triggered.emit(next);
    });
  }
}

/**
 * Opens a menu from its element: `<button lgButton [lgMenuTrigger]="actions">Actions</button>`, where `actions` is an
 * `ng-template` holding an `lg-menu`. The element says so (`aria-haspopup="menu"`, `aria-expanded`); a press, Enter,
 * Space or the down arrow open it, with focus on its first item, and it closes with focus back here.
 */
@Directive({
  selector: '[lgMenuTrigger]',
  exportAs: 'lgMenuTrigger',
  hostDirectives: [
    {
      directive: CdkMenuTrigger,
      inputs: ['cdkMenuTriggerFor: lgMenuTrigger', 'cdkMenuPosition: lgMenuPosition', 'cdkMenuTriggerData: lgMenuTriggerData'],
      outputs: ['cdkMenuOpened: lgMenuOpened', 'cdkMenuClosed: lgMenuClosed'],
    },
  ],
})
export class LgMenuTriggerDirective {
  private readonly trigger = inject(CdkMenuTrigger);

  /** Whether its menu is open. */
  isOpen(): boolean {
    return this.trigger.isOpen();
  }

  open(): void {
    this.trigger.open();
  }

  close(): void {
    this.trigger.close();
  }
}

/** The Menu, its items and its trigger, for a standalone `imports` array. */
export const LG_MENU = [LgMenuComponent, LgMenuItemComponent, LgMenuItemCheckboxComponent, LgMenuTriggerDirective] as const;
