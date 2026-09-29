import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  input,
} from '@angular/core';
import { LgHeadingComponent } from './heading.component';
import { LgSectionRuleComponent } from './section-rule.component';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';

export interface LgFolioEffect {
  value: string;
  text: string;
}

/**
 * The detail panel: emblem, title, lore, effects and one action.
 * Slots: `lgSlot="emblem"`, `lgSlot="lore"`, `lgSlot="actions"`, `lgSlot="footer"`;
 * anything else is placed after the effects.
 */
@Component({
  selector: 'lg-folio',
  imports: [LgHeadingComponent, LgSectionRuleComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
    '[class]': "align() === 'start' ? 'lg-folio lg-folio--start' : 'lg-folio'",
    role: 'complementary',
    '[attr.aria-label]': 'ariaLabel() ?? title() ?? "Details"',
  },
  template: `
    <span class="lg-folio__frame" aria-hidden="true"></span>
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
    <div class="lg-folio__scroll">
      @if (has('emblem') || eyebrow() || title()) {
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
        </div>
      }
      @if (has('lore')) {
        <p class="lg-folio__lore"><ng-content select="[lgSlot=lore]" /></p>
      }
      @if (has('lore') || effects().length) {
        <lg-section-rule variant="ornament" />
      }
      @if (effects().length) {
        <ul class="lg-folio__effects">
          @for (effect of effectRows(); track $index) {
            <li>
              @if (effect.value) {
                <b class="lg-folio__fx">{{ effect.value }}</b>
              }
              {{ effect.text }}
            </li>
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
  `,
})
export class LgFolioComponent {
  readonly title = input<string>();
  /** Lighter first words of the title ("Ember" Wolf). */
  readonly titleSub = input<string>();
  readonly eyebrow = input<string>();
  readonly effects = input<readonly (string | LgFolioEffect)[]>([]);
  /** URL of assets/CornerOrnament.svg; drawn as a gilt mask in each corner. */
  readonly cornerSrc = input<string>();
  readonly align = input<'center' | 'start'>('center');
  readonly ariaLabel = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly corners = ['tl', 'tr', 'bl', 'br'] as const;
  protected maskStyle(src: string): Record<string, string> {
    const url = `url(${src})`;
    return { 'mask-image': url, '-webkit-mask-image': url };
  }
  protected readonly effectRows = computed(() =>
    this.effects().map((effect) =>
      typeof effect === 'string' ? { value: '', text: effect } : effect,
    ),
  );

  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
