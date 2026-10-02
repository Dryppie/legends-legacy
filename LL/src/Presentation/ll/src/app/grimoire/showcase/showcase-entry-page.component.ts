import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Title } from '@angular/platform-browser';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LgPageComponent } from '@grimoire';
import { ShowcaseStore } from './showcase.store';

function cssSize(value: string | number | undefined): string | null {
  if (value === undefined || value === '') return null;
  return typeof value === 'number' ? `${value}px` : value;
}

/** One entry's page: its stories in order, or one story alone (/grimoire/<tier>/<entry>/<story>). */
@Component({
  selector: 'sc-showcase-entry-page',
  imports: [NgTemplateOutlet, RouterLink, LgPageComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (loaded(); as e) {
      <header class="sc-page__head">
        <h1 class="sc-page__title">{{ e.entry.name }}</h1>
        <p class="sc-page__summary">{{ e.entry.summary }}</p>
        <ul class="sc-page__meta">
          <li>
            Shows
            @for (c of e.entry.covers; track $index; let last = $last) {
              <code>{{ c }}</code
              >{{ last ? '' : ', ' }}
            }
          </li>
          @if (e.entry.readme) {
            <li>
              Guidelines <code>{{ e.entry.readme }}</code>
            </li>
          }
          @if (storySlug()) {
            <li>
              <a
                [routerLink]="['/grimoire', e.entry.tier, e.entry.slug]"
                queryParamsHandling="preserve"
                >All {{ e.stories.length }} stories</a
              >
            </li>
          }
        </ul>
      </header>

      @for (story of visible(); track story.slug()) {
        <section
          class="sc-story"
          [attr.aria-labelledby]="'story-' + story.slug()"
        >
          <div class="sc-story__head">
            <h2 class="sc-story__name" [id]="'story-' + story.slug()">
              {{ story.name() }}
            </h2>
            @if (!storySlug()) {
              <a
                class="sc-story__open"
                [routerLink]="[
                  '/grimoire',
                  e.entry.tier,
                  e.entry.slug,
                  story.slug(),
                ]"
                queryParamsHandling="preserve"
                >Open alone</a
              >
            }
            @if (story.notes()) {
              <p class="sc-story__notes">{{ story.notes() }}</p>
            }
          </div>
          <div class="sc-stage" [attr.data-ground]="options().ground">
            <div
              class="sc-frame lg-root"
              data-story-frame
              [attr.data-story]="
                e.entry.tier + '/' + e.entry.slug + '/' + story.slug()
              "
              [attr.data-density]="options().density || null"
              [attr.data-motion]="
                options().motion === 'reduced' ? 'reduced' : null
              "
              [class.sc-frame--flush]="story.flush()"
              [class.sc-frame--sized]="story.height() !== undefined"
              [style.width]="size(story.width())"
              [style.height]="size(story.height())"
            >
              @if (options().frame === 'page') {
                <lg-page label="Showcase frame" flow>
                  <ng-container [ngTemplateOutlet]="story.template" />
                </lg-page>
              } @else {
                <ng-container [ngTemplateOutlet]="story.template" />
              }
            </div>
          </div>
        </section>
      } @empty {
        <p class="sc-empty">
          {{ e.entry.name }} has no story called "{{ storySlug() }}".
        </p>
      }
    } @else if (store.ready()) {
      <p class="sc-empty">
        There is no showcase entry at {{ tier() }}/{{ entrySlug() }}.
      </p>
    } @else {
      <p class="sc-empty">Loading the entries…</p>
    }
  `,
})
export class ShowcaseEntryPageComponent {
  protected readonly store = inject(ShowcaseStore);
  private readonly params = toSignal(inject(ActivatedRoute).paramMap, {
    requireSync: true,
  });

  protected readonly tier = computed(() => this.params().get('tier'));
  protected readonly entrySlug = computed(() => this.params().get('entry'));
  protected readonly storySlug = computed(() => this.params().get('story'));
  protected readonly options = this.store.options;

  protected readonly loaded = computed(() =>
    this.store.find(this.tier(), this.entrySlug()),
  );
  protected readonly visible = computed(() => {
    const e = this.loaded();
    if (!e) return [];
    const one = this.storySlug();
    return one ? e.stories.filter((s) => s.slug() === one) : e.stories;
  });

  protected readonly size = cssSize;

  constructor() {
    const title = inject(Title);
    effect(() => {
      const e = this.loaded();
      if (!e) return;
      const story =
        this.visible().length === 1 && this.storySlug()
          ? ` / ${this.visible()[0].name()}`
          : '';
      title.setTitle(`${e.entry.name}${story} · Grimoire showcase`);
    });
  }
}
