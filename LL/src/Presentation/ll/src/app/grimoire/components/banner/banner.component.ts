import { ChangeDetectionStrategy, Component, ElementRef, afterNextRender, inject, input } from '@angular/core';
import { lgCheckFramed } from '../../core/grimoire-ornament';

/**
 * The Banner's headline figures, beside the identity: StatFigures, or a LevelPlate where the level is the subject.
 * They sit beside the identity whenever both fit and drop under it only when they don't (D-122).
 */
@Component({
  selector: 'lg-banner-aside',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-banner__aside' },
  template: '<ng-content />',
  styles: ':host { flex: 0 1 auto; display: flex; flex-wrap: wrap; align-items: flex-start; gap: var(--lg-space-6) var(--lg-space-8); }',
})
export class LgBannerAsideComponent {}

/** An optional footer, under the body and across the whole Banner. */
@Component({
  selector: 'lg-banner-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-banner__footer' },
  template: '<ng-content />',
  styles: ':host { display: block; position: relative; padding: 0 var(--lg-space-8) var(--lg-space-6); }',
})
export class LgBannerFooterComponent {}

/**
 * The headline block: a framed band with painted art behind it, for the one headline block on an information screen.
 * A Folio and a Banner never share a screen. The identity is the content; the headline figures go in
 * `<lg-banner-aside>` and an optional footer in `<lg-banner-footer>`. With a `label` it is a region.
 */
@Component({
  selector: 'lg-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-banner',
    '[attr.role]': "label() ? 'region' : null",
    '[attr.aria-label]': 'label() || null',
  },
  template: `
    @if (image(); as src) {
      <div
        class="lg-banner__art"
        aria-hidden="true"
        [style.background-image]="'url(' + src + ')'"
        [style.background-position]="focus() || 'center'"
      ></div>
      <div class="lg-banner__veil" aria-hidden="true"></div>
    }
    <span class="lg-banner__frame" aria-hidden="true"></span>
    @if (cornerSrc(); as src) {
      @for (corner of corners; track corner) {
        <span
          [class]="'lg-corner lg-corner--' + corner"
          [style.-webkit-mask-image]="'url(' + src + ')'"
          [style.mask-image]="'url(' + src + ')'"
          aria-hidden="true"
        ></span>
      }
    }
    <div class="lg-banner__body">
      <div class="lg-banner__main"><ng-content /></div>
      <ng-content select="lg-banner-aside" />
    </div>
    <ng-content select="lg-banner-footer" />
  `,
  styleUrls: ['./banner.component.css', '../../styles/frame-corners.css'],
})
export class LgBannerComponent {
  /** A Backgrounds asset URL. */
  readonly image = input<string>();
  readonly focus = input<string>('center');
  /** URL of assets/Ornaments/CornerOrnament.svg. */
  readonly cornerSrc = input<string>();
  readonly label = input<string>();

  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;

  constructor() {
    const host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    afterNextRender(() => lgCheckFramed(host));
  }
}

/** The Banner and its regions, for a standalone `imports` array. */
export const LG_BANNER = [LgBannerComponent, LgBannerAsideComponent, LgBannerFooterComponent] as const;
