import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  model,
} from '@angular/core';

export interface LgTab {
  id: string;
  label: string;
  count?: number;
}

/** Engraved section tabs (primary) or small-caps filters (secondary). Arrow keys move. */
@Component({
  selector: 'lg-tab-strip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'hostClass()',
    role: 'tablist',
    '[attr.aria-label]': 'label() ?? null',
    '(keydown)': 'onKeydown($event)',
  },
  template: `
    @for (tab of tabs(); track tab.id) {
      <button
        type="button"
        role="tab"
        class="lg-tabs__tab"
        [class.is-active]="tab.id === activeId()"
        [attr.aria-selected]="tab.id === activeId()"
        [attr.tabindex]="tab.id === activeId() ? 0 : -1"
        (click)="activeId.set(tab.id)"
      >
        {{ tab.label }}
        @if (tab.count !== undefined) {
          <span class="lg-tabs__count">{{ tab.count }}</span>
        }
      </button>
    }
  `,
})
export class LgTabStripComponent {
  readonly tabs = input.required<readonly LgTab[]>();
  readonly activeId = model.required<string>();
  readonly level = input<'primary' | 'secondary'>('primary');
  readonly label = input<string>();

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  protected readonly hostClass = computed(
    () => `lg-tabs lg-tabs--${this.level()}`,
  );

  protected onKeydown(event: KeyboardEvent): void {
    if (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft') return;
    const tabs = this.tabs();
    if (!tabs.length) return;
    event.preventDefault();
    const current = tabs.findIndex((tab) => tab.id === this.activeId());
    const next = (current + (event.key === 'ArrowRight' ? 1 : -1) + tabs.length) % tabs.length;
    this.activeId.set(tabs[next].id);
    this.host.nativeElement.querySelectorAll<HTMLElement>('[role=tab]')[next]?.focus();
  }
}
