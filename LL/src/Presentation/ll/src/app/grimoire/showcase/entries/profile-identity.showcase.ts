import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import {
  LgBannerComponent,
  LgButtonComponent,
  LgLevelPlateComponent,
  LgProfileFactComponent,
  LgProfileIdentityComponent,
  LgSlotDirective,
  LgStatFigureComponent,
  LgTagComponent,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-profile-identity-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgProfileIdentityComponent,
    LgProfileFactComponent,
    LgBannerComponent,
    LgButtonComponent,
    LgLevelPlateComponent,
    LgSlotDirective,
    LgStatFigureComponent,
    LgTagComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Own profile"
      width="40rem"
      notes="The eyebrow, the name with the Nobility crown, then the facts. A value can hold a link, a Tag or a link Button."
    >
      <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane" noble>
        <div lgProfileFact label="Guild">
          <a href="#" (click)="$event.preventDefault()">Emberwatch</a
          ><lg-tag>[EMB]</lg-tag>
        </div>
        <div lgProfileFact label="Essences">2 / 2 attuned</div>
        <div lgProfileFact label="Achievement Points">1,240</div>
        <div lgProfileFact label="Nobility">
          Expires <time datetime="2026-10-12">12 Oct 2026</time>
          <button
            lgButton="link"
            [attr.aria-expanded]="perks()"
            (click)="perks.set(!perks())"
          >
            {{ perks() ? 'Hide perks' : 'Show perks' }}
          </button>
        </div>
      </lg-profile-identity>
    </ng-template>

    <ng-template
      scStory="Another player"
      width="40rem"
      notes="Presence after the name and no crown. A player without a guild shows None, not an empty value."
    >
      <lg-profile-identity
        eyebrow="Viewing player"
        name="Maren"
        [presence]="{ online: false, lastSeen: '3 h ago' }"
      >
        <div lgProfileFact label="Guild">None</div>
        <div lgProfileFact label="Essences">3 / 4 attuned</div>
        <div lgProfileFact label="Achievement Points">2,860</div>
      </lg-profile-identity>
    </ng-template>

    <ng-template scStory="Another player online" width="40rem">
      <lg-profile-identity
        eyebrow="Viewing player"
        name="Maren"
        [presence]="{ online: true }"
      >
        <div lgProfileFact label="Guild">Emberwatch</div>
        <div lgProfileFact label="Essences">3 / 4 attuned</div>
      </lg-profile-identity>
    </ng-template>

    <ng-template
      scStory="Name only"
      notes="With no facts the list is left out."
    >
      <lg-profile-identity name="Pip" />
    </ng-template>

    <ng-template
      scStory="Long name"
      width="20rem"
      notes="The name and the facts wrap; nothing truncates."
    >
      <lg-profile-identity
        eyebrow="Combat Profile"
        name="Aldric Vane, Warden of the Ashen Gate"
        noble
      >
        <div lgProfileFact label="Guild">Emberwatch</div>
        <div lgProfileFact label="Essences">2 / 2 attuned</div>
        <div lgProfileFact label="Achievement Points">1,240</div>
      </lg-profile-identity>
    </ng-template>

    <ng-template
      scStory="In a Banner"
      width="62rem"
      notes="Its place: a Banner's body, with the headline figures (LevelPlate, StatFigure) in the aside."
    >
      <lg-banner label="Combat profile">
        <div lgSlot="aside" style="width: 14rem">
          <lg-level-plate
            kicker="Level"
            [level]="17"
            [xp]="8420"
            [xpMax]="12000"
            xpUnit="Combat XP"
          />
        </div>
        <lg-stat-figure
          lgSlot="aside"
          label="Combat Rating"
          value="1,284"
          caption="Permanent attributes and the equipped build"
        />
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane" noble>
          <div lgProfileFact label="Guild">
            <a href="#" (click)="$event.preventDefault()">Emberwatch</a
            ><lg-tag>[EMB]</lg-tag>
          </div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
          <div lgProfileFact label="Achievement Points">1,240</div>
        </lg-profile-identity>
      </lg-banner>
    </ng-template>
  `,
})
export class ProfileIdentityShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly perks = signal(false);
}

export const PROFILE_IDENTITY_SHOWCASE: ShowcaseEntry = {
  slug: 'profile-identity',
  name: 'ProfileIdentity',
  tier: 'game',
  summary: 'Who a player is.',
  covers: ['LgProfileIdentityComponent', 'LgProfileFactComponent'],
  readme: 'design-system/components/ProfileIdentity/README.md',
  component: ProfileIdentityShowcaseComponent,
};
