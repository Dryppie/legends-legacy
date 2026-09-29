import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** The art-filled centre of a screen. Scene art is atmosphere; overlays go in the content. */
@Component({
  selector: 'lg-stage',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-stage',
    role: 'region',
    '[attr.aria-label]': 'label() ?? null',
  },
  template: `
    @if (image(); as src) {
      <div
        class="lg-stage__art"
        aria-hidden="true"
        [style.background-image]="'url(' + src + ')'"
        [style.background-position]="focus()"
      ></div>
    }
    <div class="lg-stage__veil" aria-hidden="true"></div>
    <div class="lg-stage__content"><ng-content /></div>
  `,
})
export class LgStageComponent {
  /** A backgrounds/ or cards/ asset URL. */
  readonly image = input<string>();
  readonly focus = input('center');
  readonly label = input<string>();
}
