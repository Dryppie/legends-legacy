import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgEmblemComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-emblem-showcase',
  imports: [ShowcaseStoryDirective, LgEmblemComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Attribute signs"
      notes="Each attribute has its own point count, so the emblem becomes its sign. Flat gilt strokes, decorative only."
    >
      <div class="sc-row sc-row--start">
        @for (sign of signs; track sign.points) {
          <div class="sc-cell">
            <lg-emblem [points]="sign.points" [size]="120" />
            <p class="sc-cap">{{ sign.label }} · {{ sign.points }} points</p>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Folio size"
      notes="The default: 160px, six points, as it crowns a Folio."
    >
      <lg-emblem />
    </ng-template>

    <ng-template
      scStory="Vertex step"
      notes="The star's vertex step: by default 2 up to six points and 3 from seven."
    >
      <div class="sc-row sc-row--start">
        @for (star of steps; track $index) {
          <div class="sc-cell">
            <lg-emblem [points]="star.points" [skip]="star.skip" [size]="120" />
            <p class="sc-cap">{{ star.points }} points, step {{ star.skip }}</p>
          </div>
        }
      </div>
    </ng-template>
  `,
})
export class EmblemShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly signs = [
    { label: 'Crit Chance', points: 5 },
    { label: 'Power', points: 6 },
    { label: 'Resistance', points: 7 },
    { label: 'Armor', points: 8 },
    { label: 'Dodge', points: 9 },
  ];
  protected readonly steps = [
    { points: 8, skip: 2 },
    { points: 8, skip: 3 },
    { points: 12, skip: 5 },
  ];
}

export const EMBLEM_SHOWCASE: ShowcaseEntry = {
  slug: 'emblem',
  name: 'Emblem',
  tier: 'game',
  summary: 'The attribute sign.',
  covers: ['LgEmblemComponent'],
  readme: 'design-system/components/Emblem/README.md',
  component: EmblemShowcaseComponent,
};
