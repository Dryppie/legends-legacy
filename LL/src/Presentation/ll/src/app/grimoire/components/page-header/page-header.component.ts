import { ChangeDetectionStrategy, Component, contentChildren, input } from '@angular/core';
import { LgSlotDirective, lgHasSlot } from '../../core/grimoire-core';
import { LgHeadingComponent } from '../../primitives/heading/heading.component';
import { LgIconComponent } from '../../primitives/icon/icon.component';
import { LgIconName } from '../../core/grimoire-icons';

/**
 * The information screen heading. The current section's icon sits in a diamond (the current location). The screen's
 * actions go in `lgSlot="actions"`.
 */
@Component({
  selector: 'lg-page-header',
  imports: [LgHeadingComponent, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <header class="lg-pagehead">
      @if (icon(); as iconName) {
        <span class="lg-pagehead__icon" aria-hidden="true"
          ><svg viewBox="0 0 100 100" class="lg-pagehead__mark"><polygon points="50,1.5 98.5,50 50,98.5 1.5,50" /></svg
          ><lg-icon [name]="iconName" [size]="24"
        /></span>
      }
      <div class="lg-pagehead__text">
        @if (eyebrow()) {
          <span class="lg-pagehead__eyebrow">{{ eyebrow() }}</span>
        }
        <h1 lgHeading="screen">{{ title() }}</h1>
        @if (summary()) {
          <p class="lg-pagehead__summary">{{ summary() }}</p>
        }
      </div>
      @if (has('actions')) {
        <div class="lg-pagehead__actions"><ng-content select="[lgSlot=actions]" /></div>
      }
    </header>
  `,
})
export class LgPageHeaderComponent {
  readonly title = input.required<string>();
  /** The sidebar group: Character, World, City. */
  readonly eyebrow = input<string>();
  readonly summary = input<string>();
  readonly icon = input<LgIconName>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
