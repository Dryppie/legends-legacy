import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { LG_HEX_OUTER } from './grimoire-core';
import { LgHeadingComponent } from './heading.component';
import { LgIconComponent } from './icon.component';
import { LgIconName } from './grimoire-icons';

/** Heading row of an information screen; the screen's actions are its content. */
@Component({
  selector: 'lg-page-header',
  imports: [LgHeadingComponent, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-pagehead',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    @if (icon(); as iconName) {
      <span class="lg-pagehead__icon" aria-hidden="true">
        <svg viewBox="0 0 100 100" class="lg-pagehead__hex"><polygon [attr.points]="hex" /></svg>
        <lg-icon [name]="iconName" [size]="22" />
      </span>
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
    <div class="lg-pagehead__actions"><ng-content /></div>
  `,
})
export class LgPageHeaderComponent {
  readonly title = input.required<string>();
  /** The sidebar group: Character, World, City. */
  readonly eyebrow = input<string>();
  readonly summary = input<string>();
  readonly icon = input<LgIconName>();
  protected readonly hex = LG_HEX_OUTER;
}
