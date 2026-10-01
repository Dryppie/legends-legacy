import { ChangeDetectionStrategy, Component, computed, contentChildren, input } from '@angular/core';
import { LgSlotDirective, lgHasSlot } from './grimoire-core';
import { LgHeadingComponent } from './heading.component';
import { LgTrackComponent } from './track.component';

/** The next-step guide: the player's current journey stage, what to do now and what unlocks next. Buttons go in `lgSlot="actions"`. */
@Component({
  selector: 'lg-journey-card',
  imports: [LgHeadingComponent, LgTrackComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    style: 'display: contents',
    // `title` is an input here; keep the static attribute from becoming a native tooltip.
    '[attr.title]': 'null',
    '[attr.id]': 'null',
  },
  template: `
    <section class="lg-journey" [attr.aria-labelledby]="id() ? id() + '-title' : null">
      <div class="lg-journey__main">
        <span class="lg-journey__phase">{{ phase() }}</span>
        <h3 lgHeading="section" [attr.id]="id() ? id() + '-title' : null">{{ title() }}</h3>
        @if (summary()) {
          <p class="lg-journey__summary">{{ summary() }}</p>
        }
        @if (objective()) {
          <div class="lg-journey__objective">
            <span class="lg-journey__label">{{ objectiveLabel() || 'Recommended now' }}</span>
            <strong>{{ objective() }}</strong>
          </div>
        }
        @if (has('actions')) {
          <div class="lg-journey__actions"><ng-content select="[lgSlot=actions]" /></div>
        }
      </div>
      @if (nextUnlock()) {
        <aside class="lg-journey__unlock">
          <svg class="lg-journey__key" viewBox="0 0 24 24" width="24" height="24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <circle cx="8" cy="12" r="4" />
            <path d="M12 12h9M18 12v3M21 12v2" />
          </svg>
          <span class="lg-journey__label">{{ nextUnlockLabel() || 'Next unlock' }}</span>
          <strong>{{ nextUnlock() }}</strong>
        </aside>
      }
      @if (stages().length > 1) {
        <div class="lg-journey__track">
          <lg-track
            [steps]="stages().length"
            [current]="currentIndex()"
            [labels]="stages()"
            label="Journey"
            [startLabel]="stages()[0]"
            [endLabel]="stages()[stages().length - 1]"
          />
        </div>
      }
    </section>
  `,
})
export class LgJourneyCardComponent {
  /** The current stage's name, e.g. "Shenic Journey". */
  readonly phase = input.required<string>();
  /** All stage names in order; drawn as a Track. */
  readonly stages = input<readonly string[]>([]);
  readonly title = input.required<string>();
  readonly summary = input<string>();
  readonly objective = input<string>();
  readonly objectiveLabel = input('Recommended now');
  readonly nextUnlock = input<string>();
  readonly nextUnlockLabel = input('Next unlock');
  /** Gives the card an accessible name from its title. */
  readonly id = input<string>();

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly currentIndex = computed(() => Math.max(0, this.stages().indexOf(this.phase())));
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
