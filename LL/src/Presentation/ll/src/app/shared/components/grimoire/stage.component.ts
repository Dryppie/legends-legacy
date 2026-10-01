import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The scene backdrop: the art-filled centre of a screen. The veil — vignette, fades and film grain — belongs to the
 * art: without an image there is none. Paragraphs over the art go in a Panel.
 */
@Component({
  selector: 'lg-stage',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <section class="lg-stage" [attr.aria-label]="label() ?? null">
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
    </section>
  `,
})
export class LgStageComponent {
  /** A Backgrounds or Cards asset URL. */
  readonly image = input<string>();
  /** background-position for the art. */
  readonly focus = input<string>('center');
  readonly label = input<string>();
}
