import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  contentChildren,
  input,
  viewChild,
} from '@angular/core';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';
import { lgCheckFramed } from './grimoire-ornament';

/**
 * The headline block: a framed band with painted art behind it, for the one headline block on an information screen.
 * A Folio and a Banner never share a screen. Slots: `lgSlot="aside"` (headline figures), `lgSlot="footer"`.
 */
@Component({
  selector: 'lg-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { style: 'display: contents' },
  template: `
    <section #root class="lg-banner" [attr.aria-label]="label() ?? null">
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
            [class]="'lg-folio__corner lg-folio__corner--' + corner"
            [style.-webkit-mask-image]="'url(' + src + ')'"
            [style.mask-image]="'url(' + src + ')'"
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
    </section>
  `,
})
export class LgBannerComponent {
  /** A Backgrounds asset URL. */
  readonly image = input<string>();
  readonly focus = input<string>('center');
  /** URL of assets/Ornaments/CornerOrnament.svg. */
  readonly cornerSrc = input<string>();
  readonly label = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly root = viewChild.required<ElementRef<HTMLElement>>('root');
  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;

  constructor() {
    afterNextRender(() => lgCheckFramed(this.root().nativeElement));
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
