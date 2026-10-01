import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  contentChildren,
  input,
  viewChild,
} from '@angular/core';
import { LgRarity, LgSlotDirective, lgCx, lgHasSlot } from './grimoire-core';
import { LgHeadingComponent } from './heading.component';
import { LgSectionRuleComponent } from './section-rule.component';
import { LgTagComponent, LgTagTone } from './tag.component';
import { lgCheckFramed } from './grimoire-ornament';

export interface LgFolioEffect {
  value: string;
  text: string;
}

/**
 * The detail panel: emblem, title, lore, effects and one action. One Folio per screen.
 * Slots: `lgSlot="emblem"`, `lgSlot="lore"` (or the `lore` input), `lgSlot="actions"`, `lgSlot="footer"`; anything
 * else is placed after the effects. A Folio given a `rarity` is an item context: the title takes the rarity hue.
 */
@Component({
  selector: 'lg-folio',
  imports: [LgHeadingComponent, LgSectionRuleComponent, LgTagComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
  },
  template: `
    <aside #root [class]="classes()" [attr.aria-label]="ariaLabel() || title() || 'Details'">
      <span class="lg-folio__frame" aria-hidden="true"></span>
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
      <div class="lg-folio__scroll">
        @if (has('emblem') || eyebrow() || title() || rarity()) {
          <div class="lg-folio__top">
            @if (has('emblem')) {
              <div class="lg-folio__emblem"><ng-content select="[lgSlot=emblem]" /></div>
            }
            @if (eyebrow()) {
              <div class="lg-folio__eyebrow">{{ eyebrow() }}</div>
            }
            @if (title()) {
              <h2 lgHeading="folio" [sub]="titleSub()">{{ title() }}</h2>
            }
            @if (rarity(); as r) {
              <lg-tag [tone]="rarityTone()">{{ r }}</lg-tag>
            }
          </div>
        }
        @if (hasLore()) {
          <p class="lg-folio__lore">{{ lore() }}<ng-content select="[lgSlot=lore]" /></p>
        }
        @if (hasLore() || effects()) {
          <lg-section-rule variant="ornament" />
        }
        @if (effects(); as list) {
          <ul class="lg-folio__effects">
            @for (effect of list; track $index) {
              @if (isText(effect)) {
                <li>{{ effect }}</li>
              } @else {
                <li><b class="lg-folio__fx">{{ effect.value }}</b> {{ effect.text }}</li>
              }
            }
          </ul>
        }
        <ng-content />
        @if (has('actions')) {
          <div class="lg-folio__actions"><ng-content select="[lgSlot=actions]" /></div>
        }
      </div>
      @if (has('footer')) {
        <div class="lg-folio__footer"><ng-content select="[lgSlot=footer]" /></div>
      }
    </aside>
  `,
})
export class LgFolioComponent {
  readonly title = input<string>();
  /** Lighter first words of the title ("Ember" Wolf). */
  readonly titleSub = input<string>();
  readonly eyebrow = input<string>();
  /** One or two sentences of lore; or project it into `lgSlot="lore"`. */
  readonly lore = input<string>();
  readonly effects = input<readonly (string | LgFolioEffect)[]>();
  readonly rarity = input<LgRarity>();
  /** URL of assets/Ornaments/CornerOrnament.svg; drawn as a gilt mask in each corner. */
  readonly cornerSrc = input<string>();
  readonly align = input<'center' | 'start'>('center');
  readonly ariaLabel = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  private readonly root = viewChild.required<ElementRef<HTMLElement>>('root');
  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;
  protected readonly hasLore = computed(() => !!this.lore() || this.has('lore'));
  protected readonly rarityTone = computed(() => this.rarity()!.toLowerCase() as LgTagTone);
  protected readonly classes = computed(() => {
    const r = this.rarity();
    return lgCx('lg-folio', this.align() === 'start' && 'lg-folio--start', r && 'lg-folio--item lg-folio--' + r.toLowerCase());
  });

  constructor() {
    afterNextRender(() => lgCheckFramed(this.root().nativeElement));
  }

  protected isText(effect: string | LgFolioEffect): effect is string {
    return typeof effect === 'string';
  }

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
