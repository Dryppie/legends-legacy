import { ChangeDetectionStrategy, Component, booleanAttribute, computed, contentChild, input } from '@angular/core';
import { LgDensity, lgUniqueId } from '../../core/grimoire-core';

/** The Panel's title, in its header. It names the Panel: the Panel is a region labelled by it. */
@Component({
  selector: 'lg-panel-title',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-panel__title', '[id]': 'id()' },
  template: '<ng-content />',
  // The title takes the head's free space, so whatever follows it sits at the end.
  styles: ':host { flex: 1 1 auto; }',
})
export class LgPanelTitleComponent {
  readonly id = input(lgUniqueId('lg-panel-title'));
}

/**
 * The Panel's head: its title, then extras (a Tag, a count, a link Button), which sit at the end. Small caps over the
 * head's hairline, the Panel's one divider. `align="end"` sets the title at the end.
 */
@Component({
  selector: 'lg-panel-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-panel__head', '[attr.data-align]': "align() === 'end' ? 'end' : null" },
  template: '<ng-content />',
  styles: `
    :host {
      display: flex; align-items: center; gap: var(--lg-space-3);
      min-height: 1.875rem; padding: 0 var(--lg-panel-pad);
      border-bottom: var(--lg-border-hairline) solid var(--lg-line);
      font-size: var(--lg-text-label); line-height: var(--lg-leading-label); font-weight: 600; letter-spacing: var(--lg-tracking-caps); text-transform: uppercase; color: var(--lg-ink-muted);
    }
    :host([data-align='end']) { text-align: end; }
  `,
})
export class LgPanelHeaderComponent {
  readonly align = input<'start' | 'end'>('start');

  private readonly titleChild = contentChild(LgPanelTitleComponent);
  /** The title's id, which labels the Panel. */
  readonly titleId = computed(() => this.titleChild()?.id() ?? null);
}

/**
 * The content box: a framed group with a small-caps head.
 *
 *   <lg-panel>
 *     <lg-panel-header><lg-panel-title>Pending loot</lg-panel-title><lg-tag>4 items</lg-tag></lg-panel-header>
 *     …
 *   </lg-panel>
 *
 * With a title it is a region named by it. Without a header it is the body alone.
 */
@Component({
  selector: 'lg-panel',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-panel',
    '[class.lg-panel--flush]': 'flush()',
    '[attr.data-density]': 'density() ?? null',
    '[attr.role]': "titleId() ? 'region' : null",
    '[attr.aria-labelledby]': 'titleId()',
  },
  template: `<ng-content select="lg-panel-header" /><div class="lg-panel__body"><ng-content /></div>`,
  styleUrl: './panel.component.css',
})
export class LgPanelComponent {
  /** No body padding: for a List or table that runs edge to edge. */
  readonly flush = input(false, { transform: booleanAttribute });
  readonly density = input<LgDensity>();

  private readonly header = contentChild(LgPanelHeaderComponent);
  protected readonly titleId = computed(() => this.header()?.titleId() ?? null);
}

/** The Panel and its regions, for a standalone `imports` array. */
export const LG_PANEL = [LgPanelComponent, LgPanelHeaderComponent, LgPanelTitleComponent] as const;
