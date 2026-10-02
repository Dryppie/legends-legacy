import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LgBannerComponent,
  LgLevelPlateComponent,
  LgProfileFactComponent,
  LgProfileIdentityComponent,
  LgSlotDirective,
  LgStatFigureComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-banner-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgBannerComponent,
    LgSlotDirective,
    LgLevelPlateComponent,
    LgStatFigureComponent,
    LgProfileIdentityComponent,
    LgProfileFactComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Level plate"
      notes="Painted art under a veil, the double gilt frame and four corner ornaments; the headline figures beside the identity."
      width="64rem"
    >
      <lg-banner [image]="art" [cornerSrc]="corner" label="Combat profile">
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane">
          <div lgProfileFact label="Guild">Emberwatch</div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
        </lg-profile-identity>
        <div lgSlot="aside" style="width: 13.75rem">
          <lg-level-plate
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
      </lg-banner>
    </ng-template>

    <ng-template
      scStory="Stat figures"
      notes="Level is a figure like Combat Rating, beside the identity (D-122)."
      width="64rem"
    >
      <lg-banner
        [image]="art"
        focus="center 30%"
        [cornerSrc]="corner"
        label="Combat profile"
      >
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane" noble>
          <div lgProfileFact label="Guild">Emberwatch</div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
          <div lgProfileFact label="Achievement Points">1,240</div>
        </lg-profile-identity>
        <lg-stat-figure
          lgSlot="aside"
          label="Level"
          [value]="17"
          caption="8,420 / 12,000 Combat XP"
        />
        <lg-stat-figure
          lgSlot="aside"
          label="Combat Rating"
          value="1,284"
          caption="Permanent attributes and the equipped build"
        />
      </lg-banner>
    </ng-template>

    <ng-template
      scStory="With footer"
      notes="An optional footer runs under the body, across the whole Banner."
      width="64rem"
    >
      <lg-banner
        [image]="art"
        focus="center 30%"
        [cornerSrc]="corner"
        label="Combat profile"
      >
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane" noble>
          <div lgProfileFact label="Guild">Emberwatch</div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
        </lg-profile-identity>
        <lg-stat-figure
          lgSlot="aside"
          label="Combat Rating"
          value="1,284"
          caption="Permanent attributes and the equipped build"
        />
        <p lgSlot="footer">
          Active Nobility perks are applied automatically while Nobility is
          active.
        </p>
      </lg-banner>
    </ng-template>

    <ng-template
      scStory="Without art"
      notes="Without an image there is no veil and no grain: both belong to the art."
      width="64rem"
    >
      <lg-banner [cornerSrc]="corner" label="Combat profile">
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane">
          <div lgProfileFact label="Guild">Emberwatch</div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
        </lg-profile-identity>
        <lg-stat-figure
          lgSlot="aside"
          label="Combat Rating"
          value="1,284"
          caption="Permanent attributes and the equipped build"
        />
      </lg-banner>
    </ng-template>

    <ng-template
      scStory="Stacked"
      notes="When the figures don't fit beside the identity, they drop under it."
      width="32rem"
    >
      <lg-banner
        [image]="art"
        focus="center 30%"
        [cornerSrc]="corner"
        label="Combat profile"
      >
        <lg-profile-identity eyebrow="Combat Profile" name="Aldric Vane" noble>
          <div lgProfileFact label="Guild">Emberwatch</div>
          <div lgProfileFact label="Essences">2 / 2 attuned</div>
        </lg-profile-identity>
        <lg-stat-figure
          lgSlot="aside"
          label="Level"
          [value]="17"
          caption="8,420 / 12,000 Combat XP"
        />
        <lg-stat-figure
          lgSlot="aside"
          label="Combat Rating"
          value="1,284"
          caption="Permanent attributes and the equipped build"
        />
      </lg-banner>
    </ng-template>
  `,
})
export class BannerShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly art = 'assets/backgrounds/optimized/background.webp';
  protected readonly corner = 'assets/grimoire/corner-ornament.svg';
}

export const BANNER_SHOWCASE: ShowcaseEntry = {
  slug: 'banner',
  name: 'Banner',
  tier: 'components',
  summary: 'The headline block.',
  covers: ['LgBannerComponent'],
  readme: 'src/app/grimoire/components/banner/README.md',
  component: BannerShowcaseComponent,
};
