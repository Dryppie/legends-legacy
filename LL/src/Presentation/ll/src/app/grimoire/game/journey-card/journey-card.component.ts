import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { lgUniqueId } from '../../core/grimoire-core';
import { LgHeadingComponent } from '../../primitives/heading/heading.component';
import { LgTrackComponent } from '../../components/track/track.component';

/** The JourneyCard's actions, under its objective: a Button or two. */
@Component({
  selector: 'lg-journey-card-actions',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-journey__actions' },
  template: '<ng-content />',
  styles: ':host { display: flex; flex-wrap: wrap; gap: var(--lg-space-3); margin-top: var(--lg-space-3); }',
})
export class LgJourneyCardActionsComponent {}

/**
 * The next-step guide: the player's current journey stage, what to do now and what unlocks next. A region named by its
 * `heading`; Buttons go in an `lg-journey-card-actions`.
 */
@Component({
  selector: 'lg-journey-card',
  imports: [LgHeadingComponent, LgTrackComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'lg-journey', role: 'region', '[attr.aria-labelledby]': 'headingId' },
  template: `
    <div class="lg-journey__main">
      <span class="lg-journey__phase">{{ phase() }}</span>
      <h3 lgHeading="section" [attr.id]="headingId">{{ heading() }}</h3>
      @if (summary()) {
        <p class="lg-journey__summary">{{ summary() }}</p>
      }
      @if (objective()) {
        <div class="lg-journey__objective">
          <span class="lg-journey__label">{{ objectiveLabel() || 'Recommended now' }}</span>
          <strong>{{ objective() }}</strong>
        </div>
      }
      <ng-content select="lg-journey-card-actions" />
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
  `,
  styleUrl: './journey-card.component.css',
})
export class LgJourneyCardComponent {
  /** The current stage's name, e.g. "Shenic Journey". */
  readonly phase = input.required<string>();
  /** All stage names in order; drawn as a Track. */
  readonly stages = input<readonly string[]>([]);
  /** The journey's title: the card is a region named by it. */
  readonly heading = input.required<string>();
  readonly summary = input<string>();
  readonly objective = input<string>();
  readonly objectiveLabel = input('Recommended now');
  readonly nextUnlock = input<string>();
  readonly nextUnlockLabel = input('Next unlock');

  protected readonly headingId = lgUniqueId('lgjc') + '-heading';
  protected readonly currentIndex = computed(() => Math.max(0, this.stages().indexOf(this.phase())));
}
