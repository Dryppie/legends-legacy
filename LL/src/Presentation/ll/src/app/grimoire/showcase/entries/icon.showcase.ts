import { ChangeDetectionStrategy, Component } from '@angular/core';
import {
  LG_ICON_MARKERS,
  LG_ICON_NAMES,
  LgIconComponent,
  LgIconName,
} from '../../../shared/components/grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-icon-showcase',
  imports: [ShowcaseStoryDirective, LgIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Set"
      notes="Every icon in the set at 20px, the default size. Each takes currentColor."
      width="52rem"
    >
      <div class="sc-grid" style="--sc-cell: 7rem">
        @for (name of names; track name) {
          <div class="sc-cell">
            <lg-icon [name]="name" [size]="20" />
            <p class="sc-cap">{{ name }}</p>
          </div>
        }
      </div>
    </ng-template>

    <ng-template
      scStory="Sizes"
      notes="icon-lg 24px for display, icon-md 20px by default, icon-sm 16px inline and in Compact, and icon-marker 12px for solid markers only."
    >
      <div class="sc-col">
        @for (size of lineSizes; track size.px) {
          <div class="sc-cell">
            <p class="sc-cap">{{ size.px }}px · {{ size.token }}</p>
            <div class="sc-row">
              @for (name of lines; track name) {
                <lg-icon [name]="name" [size]="size.px" />
              }
            </div>
          </div>
        }
        <div class="sc-cell">
          <p class="sc-cap">12px · icon-marker</p>
          <div class="sc-row">
            @for (name of markers; track name) {
              <lg-icon [name]="name" [size]="12" />
            }
          </div>
        </div>
      </div>
    </ng-template>

    <ng-template
      scStory="Lock marker"
      notes="A closed padlock at 12px, drawn solid so it holds at that size. The NavRail shows it on a locked destination in place of the word."
    >
      <div class="sc-row">
        <span
          ><lg-icon name="colosseum" [size]="20" /> Colosseum
          <lg-icon name="lock" [size]="12" title="Locked"
        /></span>
        <lg-icon name="lock" [size]="12" />
        <lg-icon name="lock" [size]="16" />
      </div>
    </ng-template>

    <ng-template
      scStory="Nobility marker"
      notes="The filled crown of the Nobility mark: 12px beside a name, 16px beside a screen title."
    >
      <div class="sc-row">
        <span
          ><lg-icon name="nobility" [size]="12" title="Active Nobility" />
          Aldric Vane</span
        >
        <lg-icon name="nobility" [size]="16" />
      </div>
    </ng-template>

    <ng-template
      scStory="With a title"
      notes="A standalone icon that means something gets a title: it becomes an image named by it. Without one it is hidden, since the label beside it names the thing."
    >
      <div class="sc-row">
        <lg-icon name="inventory" [size]="16" title="Inventory" />
        <lg-icon name="settings" title="Settings" />
        <lg-icon name="world-map" [size]="24" title="World Map" />
      </div>
    </ng-template>

    <ng-template
      scStory="Unknown name"
      notes="An unknown name draws nothing and logs a console warning; the label beside it is the fallback."
    >
      <span><lg-icon [name]="unknownIcon" /> Bestiary</span>
    </ng-template>
  `,
})
export class IconShowcaseComponent extends ShowcaseEntryComponent {
  protected readonly names = LG_ICON_NAMES;
  protected readonly markers = LG_ICON_MARKERS;
  protected readonly lines = LG_ICON_NAMES.filter(
    (name) => !LG_ICON_MARKERS.includes(name),
  );
  protected readonly lineSizes = [
    { px: 24, token: 'icon-lg' },
    { px: 20, token: 'icon-md' },
    { px: 16, token: 'icon-sm' },
  ] as const;
  /** Not in the set: the icon draws nothing. */
  protected readonly unknownIcon = 'bestiary' as LgIconName;
}

export const ICON_SHOWCASE: ShowcaseEntry = {
  slug: 'icon',
  name: 'Icon',
  tier: 'primitives',
  summary: "The game's icon set.",
  covers: ['LgIconComponent'],
  readme: 'design-system/components/Icon/README.md',
  component: IconShowcaseComponent,
};
