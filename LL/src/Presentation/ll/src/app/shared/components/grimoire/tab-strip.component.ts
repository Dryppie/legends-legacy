import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { LgDensity } from './grimoire-core';

export interface LgTab {
  id: string;
  label: string;
  count?: number;
}

/** The tabs: engraved section tabs (primary) or small-caps filters (secondary). Left and Right move. */
@Component({
  selector: 'lg-tab-strip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <div
      [class]="'lg-tabs lg-tabs--' + (level() || 'primary')"
      role="tablist"
      [attr.aria-label]="label() ?? null"
      [attr.data-density]="density() ?? null"
      (keydown)="onKeydown($event)"
    >
      @for (tab of tabs(); track tab.id) {
        <button
          type="button"
          role="tab"
          [attr.aria-selected]="tab.id === activeId()"
          [attr.tabindex]="tab.id === activeId() ? 0 : -1"
          class="lg-tabs__tab"
          [class.is-active]="tab.id === activeId()"
          (click)="activeId.set(tab.id)"
        >{{ tab.label }}@if (tab.count != null) {<span class="lg-tabs__count">{{ tab.count }}</span>}</button>
      }
    </div>
  `,
})
export class LgTabStripComponent {
  readonly tabs = input.required<readonly LgTab[]>();
  /** The active tab; two-way bindable (React's activeId and onChange). */
  readonly activeId = model.required<string>();
  readonly level = input<'primary' | 'secondary'>('primary');
  readonly density = input<LgDensity>();
  readonly label = input<string>();

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    const tabs = this.tabs();
    if (!tabs.length) return;
    event.preventDefault();
    const i = tabs.findIndex((tab) => tab.id === this.activeId());
    const j = (i + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
    this.activeId.set(tabs[j].id);
    (event.currentTarget as HTMLElement).querySelectorAll<HTMLElement>('[role=tab]')[j]?.focus();
  }
}
