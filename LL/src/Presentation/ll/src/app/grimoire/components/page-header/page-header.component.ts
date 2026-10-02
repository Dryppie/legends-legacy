import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { LG_SHELL } from '../../core/grimoire-core';
import { LgHeadingComponent } from '../../primitives/heading/heading.component';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LgIconName } from '../../core/grimoire-icons';

/** The screen's actions, at the end of the PageHeader: a SearchField, Buttons. They wrap under the title below Medium. */
@Component({
  selector: 'lg-page-header-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-pagehead__actions' },
  template: '<ng-content />',
  styles: `
    :host { display: flex; flex-wrap: nowrap; align-items: center; gap: var(--lg-space-2); margin-left: auto; min-width: 0; }
    @container lg-region (width < 44rem) { :host { flex-wrap: wrap; width: 100%; margin-left: 0; } }
  `,
})
export class LgPageHeaderActionsComponent {}

/**
 * The information screen heading. The current section's icon sits in a diamond (the current location). The screen's
 * actions go in `<lg-page-header-actions>`. Inside a GameShell it is one dense row (D-120).
 */
@Component({
  selector: 'lg-page-header',
  imports: [LgHeadingComponent, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-pagehead', '[class.lg-pagehead--dense]': 'dense' },
  template: `
    @if (icon(); as iconName) {
      <span class="lg-pagehead__icon" aria-hidden="true"
        ><svg viewBox="0 0 100 100" class="lg-pagehead__mark"><polygon points="50,1.5 98.5,50 50,98.5 1.5,50" /></svg
        ><span class="lg-pagehead__glyph"><lg-icon [name]="iconName" [size]="24" /></span
      ></span>
    }
    <div class="lg-pagehead__text">
      @if (eyebrow()) {
        <span class="lg-pagehead__eyebrow">{{ eyebrow() }}</span>
      }
      <h1 lgHeading="screen">{{ heading() }}</h1>
      @if (summary()) {
        <p class="lg-pagehead__summary">{{ summary() }}</p>
      }
    </div>
    <ng-content select="lg-page-header-actions" />
  `,
  styleUrl: './page-header.component.css',
})
export class LgPageHeaderComponent {
  readonly heading = input.required<string>();
  /** The sidebar group: Character, World, City. */
  readonly eyebrow = input<string>();
  readonly summary = input<string>();
  readonly icon = input<LgIconName>();

  /** In the game's frame (D-120). */
  protected readonly dense = !!inject(LG_SHELL, { optional: true });
}

/** The PageHeader and its region, for a standalone `imports` array. */
export const LG_PAGE_HEADER = [LgPageHeaderComponent, LgPageHeaderActionsComponent] as const;
