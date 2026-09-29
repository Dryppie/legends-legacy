import {
  ChangeDetectionStrategy,
  Component,
  contentChildren,
  input,
} from '@angular/core';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';

/**
 * Framed band with painted art behind it, for the one headline block on an
 * information screen. Slots: `lgSlot="aside"` (headline figures), `lgSlot="footer"`.
 */
@Component({
  selector: 'lg-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-banner',
    role: 'region',
    '[attr.aria-label]': 'label() ?? null',
  },
  template: `
    @if (image(); as src) {
      <div
        class="lg-banner__art"
        aria-hidden="true"
        [style.background-image]="'url(' + src + ')'"
        [style.background-position]="focus()"
      ></div>
    }
    <div class="lg-banner__veil" aria-hidden="true"></div>
    <span class="lg-banner__frame" aria-hidden="true"></span>
    @if (cornerSrc(); as src) {
      @for (corner of corners; track corner) {
        <span
          class="lg-folio__corner"
          [class]="'lg-folio__corner--' + corner"
          [style]="maskStyle(src)"
          aria-hidden="true"
        ></span>
      }
    }
    <div class="lg-banner__body">
      <div class="lg-banner__main"><ng-content /></div>
      @if (has('aside')) {
        <div class="lg-banner__aside"><ng-content select="[lgSlot=aside]" /></div>
      }
    </div>
    @if (has('footer')) {
      <div class="lg-banner__footer"><ng-content select="[lgSlot=footer]" /></div>
    }
  `,
})
export class LgBannerComponent {
  /** A backgrounds/ asset URL. */
  readonly image = input<string>();
  readonly focus = input('center');
  /** URL of assets/CornerOrnament.svg. */
  readonly cornerSrc = input<string>();
  readonly label = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;
  protected maskStyle(src: string): Record<string, string> {
    const url = `url(${src})`;
    return { 'mask-image': url, '-webkit-mask-image': url };
  }
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
