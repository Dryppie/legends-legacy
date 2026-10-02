import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ShowcaseStore } from './showcase.store';
import { SHOWCASE_TIERS } from './showcase.types';

/** The showcase's first page: what it is for, and every entry and story. The snapshot run reads its story links. */
@Component({
  selector: 'sc-showcase-home',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <header class="sc-page__head">
      <h1 class="sc-page__title">Grimoire showcase</h1>
      <p class="sc-page__summary">
        Every Grimoire component on its own, outside the game, in each state it
        supports. Use the toolbar to change density, text size, motion, frame
        and ground. Each story has its own address, which the snapshot run
        (<code>npm run grimoire:snapshots</code>) captures.
      </p>
    </header>
    @if (store.ready()) {
      @for (tier of tiers; track tier.id) {
        <h2 class="sc-toc__tier">{{ tier.label }}</h2>
        <ul class="sc-toc">
          @for (e of store.inTier(tier.id); track e.entry.slug) {
            <li>
              <a
                [routerLink]="['/grimoire', e.entry.tier, e.entry.slug]"
                queryParamsHandling="preserve"
                >{{ e.entry.name }}</a
              >
              <span class="sc-muted"> {{ e.entry.summary }}</span>
              <ul class="sc-toc__stories">
                @for (s of e.stories; track s.slug()) {
                  <li>
                    <a
                      data-story-link
                      [attr.data-snapshot]="s.noSnapshot() ? 'false' : 'true'"
                      [routerLink]="[
                        '/grimoire',
                        e.entry.tier,
                        e.entry.slug,
                        s.slug(),
                      ]"
                      queryParamsHandling="preserve"
                      >{{ s.name() }}</a
                    >
                  </li>
                }
              </ul>
            </li>
          }
        </ul>
      }
    } @else {
      <p class="sc-empty">Loading the entries…</p>
    }
  `,
})
export class ShowcaseHomeComponent {
  protected readonly store = inject(ShowcaseStore);
  protected readonly tiers = SHOWCASE_TIERS;
}
