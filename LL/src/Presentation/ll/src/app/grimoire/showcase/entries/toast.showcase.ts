import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import {
  LgButtonComponent,
  LgToastComponent,
  LgToaster,
  LgToastOutletComponent,
} from '@grimoire';
import {
  ShowcaseEntryComponent,
  ShowcaseStoryDirective,
} from '../showcase-story.directive';
import { ShowcaseEntry } from '../showcase.types';

@Component({
  selector: 'sc-toast-showcase',
  imports: [
    ShowcaseStoryDirective,
    LgToastComponent,
    LgToastOutletComponent,
    LgButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ng-template
      scStory="Tones"
      width="24rem"
      notes="A Level 2 float with a start bar in its tone; the words carry the meaning and the bar backs them."
    >
      <div class="sc-col">
        <lg-toast heading="Settings saved" tone="success" />
        <lg-toast heading="Bazaar updated" text="3 new listings" />
        <lg-toast heading="Teleport ready in 4m 12s" tone="warning" />
        <lg-toast
          heading="Couldn't save. Your change was undone."
          tone="danger"
        />
      </div>
    </ng-template>

    <ng-template
      scStory="With an action"
      width="24rem"
      notes="One action, a link Button, before Dismiss. It is never the only way to do it."
    >
      <lg-toast
        heading="Sold Ashen Blade"
        text="For 1,200 Cinders"
        tone="success"
        action="Undo"
      />
    </ng-template>

    <ng-template
      scStory="Long words"
      width="18rem"
      notes="The words wrap; the buttons stay on the heading's line."
    >
      <lg-toast
        heading="Your guild's raid on the Hollow Cathedral starts in 5 minutes"
        text="Join from the Raid screen before the doors close."
        action="View"
      />
    </ng-template>

    <ng-template
      scStory="Show toasts"
      height="20rem"
      notes="Interactive: toasts show at the top centre of their lg-toast-outlet, here under the buttons, newest on top, three at most; each stays at least 6 seconds and waits while hovered or focused. Escape dismisses the one focus is in."
    >
      <div>
        <div class="sc-row">
          <button lgButton size="sm" (click)="saved()">Save</button>
          <button lgButton size="sm" (click)="sold()">Sell</button>
          <button lgButton size="sm" (click)="failed()">Fail to save</button>
        </div>
        <lg-toast-outlet />
      </div>
    </ng-template>
  `,
})
export class ToastShowcaseComponent extends ShowcaseEntryComponent {
  private readonly toaster = inject(LgToaster);

  protected saved(): void {
    this.toaster.show({ heading: 'Settings saved', tone: 'success' });
  }
  protected sold(): void {
    this.toaster.show({
      heading: 'Sold Ashen Blade',
      text: 'For 1,200 Cinders',
      tone: 'success',
      action: 'Undo',
    });
  }
  protected failed(): void {
    this.toaster.show({
      heading: "Couldn't save. Your change was undone.",
      tone: 'danger',
    });
  }
}

export const TOAST_SHOWCASE: ShowcaseEntry = {
  slug: 'toast',
  name: 'Toast',
  tier: 'primitives',
  summary:
    'A brief outcome at the top centre of the stage, announced, never taking focus.',
  covers: [
    'LgToastComponent',
    'LgToastOutletComponent',
    'LgToastStackComponent',
  ],
  readme: 'src/app/grimoire/primitives/toast/README.md',
  component: ToastShowcaseComponent,
};
