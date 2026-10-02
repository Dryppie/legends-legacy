import {
  ChangeDetectionStrategy,
  Component,
  computed,
  signal,
} from '@angular/core';
import { LgButtonComponent, LgSearchFieldComponent } from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

const PLAYERS = [
  'Maren',
  'Marek',
  'Mareth',
  'Kaelen',
  'Oswin',
  'Tamsin',
  'Pip',
];

/*
 * The suggestion panel opens only when the input has focus or is typed in (its open state is internal), so no story
 * can show it open on load: the stories with suggestions open on focus.
 */
@Component({
  selector: 'sc-search-field-showcase',
  imports: [ShowcaseStoryDirective, LgSearchFieldComponent, LgButtonComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Empty"
      notes="Named by its label, or its placeholder."
      width="22.5rem"
    >
      <lg-search-field
        placeholder="Search character by name…"
        label="Search character by name"
      />
    </ng-template>

    <ng-template scStory="With value" width="22.5rem">
      <lg-search-field
        value="Mar"
        placeholder="Search character by name…"
        label="Search character by name"
      />
    </ng-template>

    <ng-template
      scStory="Suggestions"
      notes="Interactive: focus the field or type two letters to open the suggestions; arrows and Enter pick, Escape closes."
      width="22.5rem"
      height="14rem"
    >
      <div>
        <lg-search-field
          [(value)]="query"
          [suggestions]="matches()"
          [searched]="query().length >= 2"
          placeholder="Search character by name…"
          label="Search character by name"
          (pick)="picked.set($event)"
        />
        <p class="sc-cap">
          {{
            picked()
              ? 'Selected: ' + picked()
              : 'Type two letters; arrows and Enter pick.'
          }}
        </p>
      </div>
    </ng-template>

    <ng-template
      scStory="No results"
      notes="Interactive: focus the field. A search that found nothing says so."
      width="22.5rem"
      height="10rem"
    >
      <lg-search-field
        value="Zed"
        [suggestions]="[]"
        searched
        placeholder="Search character by name…"
        label="Search character by name"
      />
    </ng-template>

    <ng-template
      scStory="Loading"
      notes="Interactive: focus the field. While the search runs, the panel says so."
      width="22.5rem"
      height="10rem"
    >
      <lg-search-field
        value="Kae"
        loading
        placeholder="Search character by name…"
        label="Search character by name"
      />
    </ng-template>

    <ng-template
      scStory="With search button"
      notes="Pair it with an explicit Search button; both take the control height of their region."
      width="30rem"
    >
      <div class="sc-row">
        <lg-search-field
          placeholder="Search character by name…"
          label="Search character by name"
        />
        <button lgButton size="sm">Search</button>
      </div>
    </ng-template>
  `,
})
export class SearchFieldShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly query = signal('Ma');
  protected readonly picked = signal('');
  protected readonly matches = computed(() => {
    const q = this.query().toLowerCase();
    return q.length >= 2
      ? PLAYERS.filter((n) => n.toLowerCase().startsWith(q))
      : [];
  });
}

export const SEARCH_FIELD_SHOWCASE: ShowcaseEntry = {
  slug: 'search-field',
  name: 'SearchField',
  tier: 'components',
  summary: 'Search with suggestions.',
  covers: ['LgSearchFieldComponent'],
  readme: 'src/app/grimoire/components/search-field/README.md',
  component: SearchFieldShowcaseComponent,
};
