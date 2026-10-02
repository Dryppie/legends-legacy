import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  afterNextRender,
  computed,
  contentChild,
  inject,
  input,
} from '@angular/core';
import { LgRarity, lgCx } from '../../core/grimoire-core';
import { LgHeadingComponent } from '../../primitives/heading/heading.component';
import { LgSectionRuleComponent } from '../../primitives/section-rule/section-rule.component';
import { LgTagComponent, LgTagTone } from '../../primitives/tag/tag.component';
import { lgCheckFramed } from '../../core/grimoire-ornament';

export interface LgFolioEffect {
  value: string;
  text: string;
}

/** The Folio's emblem, above its title: an Emblem, or an ItemSlot for an item. */
@Component({
  selector: 'lg-folio-emblem',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-folio__emblem' },
  template: '<ng-content />',
  styles: ':host { display: grid; place-items: center; margin-bottom: var(--lg-space-2); }',
})
export class LgFolioEmblemComponent {}

/** One or two sentences of lore, with its key nouns in `<b>`: the lore type style in ink-muted, bold words in ink. */
@Component({
  selector: 'lg-folio-lore',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-folio__lore lg-type-lore' },
  template: '<ng-content />',
  styles: ':host { display: block; color: var(--lg-ink-muted); }',
})
export class LgFolioLoreComponent {}

/** The Folio's action: one Button, two at most. */
@Component({
  selector: 'lg-folio-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-folio__actions' },
  template: '<ng-content />',
  styles: ':host { display: flex; flex-wrap: wrap; justify-content: var(--lg-folio-justify, center); gap: var(--lg-space-3); }',
})
export class LgFolioActionsComponent {}

/** The Folio's footer, under a hairline: a Track. */
@Component({
  selector: 'lg-folio-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-folio__footer' },
  template: '<ng-content />',
  styles: `
    :host { display: block; position: relative; padding: var(--lg-space-4) var(--lg-space-8) var(--lg-space-6); border-top: var(--lg-border-hairline) solid var(--lg-line); }
  `,
})
export class LgFolioFooterComponent {}

/**
 * The detail panel: emblem, heading, lore, effects and one action. One Folio per screen.
 *
 *   <lg-folio eyebrow="Attribute" heading="Power" [effects]="effects" [cornerSrc]="corner">
 *     <lg-folio-emblem><lg-emblem [points]="6" /></lg-folio-emblem>
 *     <lg-folio-lore>Raw force behind every blow. With <b>Power</b> …</lg-folio-lore>
 *     <lg-folio-actions><button lgButton>View breakdown</button></lg-folio-actions>
 *     <lg-folio-footer><lg-track … /></lg-folio-footer>
 *   </lg-folio>
 *
 * Other content is placed after the effects. A Folio given a `rarity` is an item context: the heading takes the rarity
 * hue. It is Comfortable, and a region of its own.
 */
@Component({
  selector: 'lg-folio',
  imports: [LgHeadingComponent, LgSectionRuleComponent, LgTagComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '[class]': 'classes()',
    role: 'complementary',
    '[attr.aria-label]': "label() || heading() || 'Details'",
    'data-density': 'comfortable',
  },
  template: `
    <span class="lg-folio__frame" aria-hidden="true"></span>
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
    <div class="lg-folio__scroll">
      @if (emblem() || eyebrow() || heading() || rarity()) {
        <div class="lg-folio__top">
          <ng-content select="lg-folio-emblem" />
          @if (eyebrow()) {
            <div class="lg-folio__eyebrow">{{ eyebrow() }}</div>
          }
          @if (heading()) {
            <h2 lgHeading="folio" [sub]="headingSub()">{{ heading() }}</h2>
          }
          @if (rarity(); as r) {
            <lg-tag [tone]="rarityTone()">{{ r }}</lg-tag>
          }
        </div>
      }
      <ng-content select="lg-folio-lore" />
      @if (lore() || effects()) {
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
      <ng-content select="lg-folio-actions" />
    </div>
    <ng-content select="lg-folio-footer" />
  `,
  styleUrls: ['./folio.component.css', '../../styles/frame-corners.css'],
})
export class LgFolioComponent {
  readonly heading = input<string>();
  /** Lighter first words of the heading ("Ember" Wolf). */
  readonly headingSub = input<string>();
  readonly eyebrow = input<string>();
  readonly effects = input<readonly (string | LgFolioEffect)[]>();
  readonly rarity = input<LgRarity>();
  /** URL of assets/Ornaments/CornerOrnament.svg; drawn as a gilt mask in each corner. */
  readonly cornerSrc = input<string>();
  readonly align = input<'center' | 'start'>('center');
  /** What the Folio is called for screen readers; its heading, or "Details", by default. */
  readonly label = input<string>();

  protected readonly emblem = contentChild(LgFolioEmblemComponent);
  protected readonly lore = contentChild(LgFolioLoreComponent);
  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;
  protected readonly rarityTone = computed(() => this.rarity()!.toLowerCase() as LgTagTone);
  protected readonly classes = computed(() => {
    const r = this.rarity();
    return lgCx('lg-folio', this.align() === 'start' && 'lg-folio--start', r && 'lg-folio--item lg-folio--' + r.toLowerCase());
  });

  constructor() {
    const host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    afterNextRender(() => lgCheckFramed(host));
  }

  protected isText(effect: string | LgFolioEffect): effect is string {
    return typeof effect === 'string';
  }
}

/** The Folio and its regions, for a standalone `imports` array. */
export const LG_FOLIO = [
  LgFolioComponent,
  LgFolioEmblemComponent,
  LgFolioLoreComponent,
  LgFolioActionsComponent,
  LgFolioFooterComponent,
] as const;
