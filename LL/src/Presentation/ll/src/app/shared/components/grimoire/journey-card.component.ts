import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChildren,
  input,
} from '@angular/core';
import { LgHeadingComponent } from './heading.component';
import { LgSlotDirective, lgHasSlot, lgUniqueId } from './grimoire-core';
import { LgTrackComponent } from './track.component';

/**
 * The player's guided next step. Maps onto the player-journey guidance model;
 * buttons go in `lgSlot="actions"`.
 */
@Component({
  selector: 'lg-journey-card',
  imports: [LgHeadingComponent, LgTrackComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'lg-journey',
    role: 'region',
    '[attr.aria-labelledby]': 'id + "-title"',
  },
  template: `
    <span class="lg-journey__frame" aria-hidden="true"></span>
    <div class="lg-journey__main">
      <span class="lg-journey__phase">{{ phase() }}</span>
      <h2 lgHeading="section" [id]="id + '-title'">{{ heading() }}</h2>
      @if (summary()) {
        <p class="lg-journey__summary">{{ summary() }}</p>
      }
      @if (objective()) {
        <div class="lg-journey__objective">
          <span class="lg-journey__label">{{ objectiveLabel() }}</span>
          <strong>{{ objective() }}</strong>
        </div>
      }
      @if (has('actions')) {
        <div class="lg-journey__actions"><ng-content select="[lgSlot=actions]" /></div>
      }
    </div>
    @if (nextUnlock()) {
      <aside class="lg-journey__unlock">
        <svg class="lg-journey__key" viewBox="0 0 24 24" width="22" height="22" fill="none"
          stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
          <circle cx="8" cy="12" r="4" />
          <path d="M12 12h9M18 12v3M21 12v2" />
        </svg>
        <span class="lg-journey__label">{{ nextUnlockLabel() }}</span>
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
})
export class LgJourneyCardComponent {
  /** Current stage name, e.g. "Shenic Journey". */
  readonly phase = input.required<string>();
  /** All stage names in order; drawn as a Track. */
  readonly stages = input<readonly string[]>([]);
  readonly heading = input.required<string>();
  readonly summary = input<string>();
  readonly objective = input<string>();
  readonly objectiveLabel = input('Recommended now');
  readonly nextUnlock = input<string>();
  readonly nextUnlockLabel = input('Next unlock');

  private readonly slots = contentChildren(LgSlotDirective);
  protected readonly id = lgUniqueId('lg-journey');
  protected readonly currentIndex = computed(() =>
    Math.max(0, this.stages().indexOf(this.phase())),
  );
  protected has(name: string): boolean {
    return lgHasSlot(this.slots(), name);
  }
}
