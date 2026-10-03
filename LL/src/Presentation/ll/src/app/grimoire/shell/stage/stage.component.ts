import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The scene backdrop: the art-filled centre of a screen. The veil — vignette, fades and film grain — belongs to the
 * art: without an image there is none. Paragraphs over the art go in a Panel. Given a `label` it is a region.
 */
@Component({
  selector: 'lg-stage',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-stage',
    '[attr.role]': "label() ? 'region' : null",
    '[attr.aria-label]': 'label() || null',
  },
  template: `
    @if (image(); as src) {
      <div
        class="lg-stage__art"
        aria-hidden="true"
        [style.background-image]="'url(' + src + ')'"
        [style.background-position]="focus() || 'center'"
      ></div>
      <div class="lg-stage__veil" aria-hidden="true"></div>
    }
    <div class="lg-stage__content"><ng-content /></div>
  `,
  styleUrl: './stage.component.css',
})
export class LgStageComponent {
  /** A Backgrounds or Cards asset URL. */
  readonly image = input<string>();
  /** background-position for the art. */
  readonly focus = input<string>('center');
  readonly label = input<string>();
}
