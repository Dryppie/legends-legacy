import { ChangeDetectionStrategy, Component } from '@angular/core';
import { LgPresenceComponent } from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-presence-showcase',
  imports: [ShowcaseStoryDirective, LgPresenceComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Online"
      notes="Arcana words and a filled dot. The words carry the meaning, not the dot."
    >
      <lg-presence [online]="true" />
    </ng-template>

    <ng-template
      scStory="Offline"
      notes="Muted words, a hollow dot, and when they were last seen."
    >
      <lg-presence [online]="false" lastSeen="3 h ago" />
    </ng-template>

    <ng-template
      scStory="Compact"
      notes="Drops the Last seen prefix, for tables."
    >
      <lg-presence [online]="false" lastSeen="2 d ago" compact />
    </ng-template>

    <ng-template scStory="Unknown" notes="With no last-seen time.">
      <div class="sc-row">
        <lg-presence [online]="false" />
        <lg-presence [online]="false" compact />
      </div>
    </ng-template>
  `,
})
export class PresenceShowcaseComponent extends ShowcaseEntryComponent {}

export const PRESENCE_SHOWCASE: ShowcaseEntry = {
  slug: 'presence',
  name: 'Presence',
  tier: 'game',
  summary: 'The online status.',
  covers: ['LgPresenceComponent'],
  readme: 'design-system/components/Presence/README.md',
  component: PresenceShowcaseComponent,
};
