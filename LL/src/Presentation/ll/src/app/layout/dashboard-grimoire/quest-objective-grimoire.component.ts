import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  TemplateRef,
  ViewContainerRef,
  effect,
  inject,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { Overlay, OverlayRef } from '@angular/cdk/overlay';
import { Router } from '@angular/router';
import { TemplatePortal } from '@angular/cdk/portal';
import { QuestTrackerComponent } from '../dashboard/quest-tracker/quest-tracker.component';
import { DialogFocusDirective } from '../../shared/directives/dialog-focus/dialog-focus.directive';
import { LgButtonComponent, LgObjectiveComponent, LgSlotDirective } from '../../shared/components/grimoire';

/**
 * The pinned quest in the TopBar's centre (D-110): the old header tracker's data and behaviour as an Objective, its
 * full tracker in the Objective's popover, and the first-session welcome dialog as before — rendered at the page's
 * root, outside the shell, so it covers the whole game.
 */
@Component({
  selector: 'app-quest-objective-grimoire',
  imports: [LgObjectiveComponent, LgButtonComponent, LgSlotDirective, DialogFocusDirective],
  changeDetection: ChangeDetectionStrategy.Default,
  styleUrls: ['../dashboard/quest-tracker/quest-tracker.component.scss', './quest-objective-grimoire.component.scss'],
  host: { style: 'display: contents' },
  template: `
    @if (quest(); as q) {
      <span
        #questHeader
        class="qo-host"
        [class.quest-header-awaiting-welcome]="welcomeOpen()"
        [class.quest-header-welcome-reveal]="welcomeRevealing()"
      >
        <lg-objective
          kicker="Quest"
          [title]="q.title"
          [objective]="summaryText()"
          [current]="requiresChoice() ? undefined : objective()?.currentAmount"
          [required]="requiresChoice() ? undefined : objective()?.requiredAmount"
          [(open)]="open"
        >
          <div lgSlot="panel" class="qo-panel">
            <div class="qo-head">
              <div class="qo-titles">
                <p class="qo-kicker">Pinned quest</p>
                <p class="qo-title">{{ q.title }}</p>
                @if (q.chain; as chain) {
                  <p class="qo-chain">{{ chain.title }} · Chain {{ chain.step }} of {{ chain.totalSteps }}</p>
                }
              </div>
              <span class="qo-count">{{ completedObjectiveCount() }} / {{ q.objectives.length }} objectives</span>
            </div>
            @if (q.chain; as chain) {
              <p class="qo-note">{{ chain.description }}</p>
            }
            @if (requiresChoice()) {
              <p class="qo-note">{{ q.choice?.selectionSummary }}</p>
            } @else {
              <ol class="qo-list">
                @for (o of q.objectives; track $index) {
                  <li class="qo-item" [class.is-done]="o.isCompleted">
                    <span class="qo-mark" aria-hidden="true">{{ o.isCompleted ? '✓' : $index + 1 }}</span>
                    <span class="qo-text">{{ o.description }}@if (o.isCompleted) {<span class="lg-sr">, done</span>}</span>
                    <span class="qo-amount">{{ o.currentAmount }} / {{ o.requiredAmount }}</span>
                  </li>
                }
              </ol>
            }
            @if (actionLabel(); as label) {
              <div class="qo-actions">
                <button [lgButton]="readyToTurnIn() ? 'solid' : 'primary'" size="sm" (click)="open.set(false); act()">
                  {{ label }}
                </button>
              </div>
            }
            @if (error(); as message) {
              <p class="qo-error" role="alert">{{ message }}</p>
            }
          </div>
        </lg-objective>
      </span>
    }

    <ng-template #welcomeTpl>
      @if (quest()) {
        <div class="ll-modal-backdrop quest-welcome-backdrop" [class.quest-welcome-transitioning]="welcomeTransitioning()">
          <section
            #welcomePanel
            appDialogFocus
            class="ll-modal-panel quest-welcome-panel overflow-hidden p-0"
            [dialogEscapeDisabled]="true"
            aria-labelledby="quest-welcome-title"
            aria-describedby="quest-welcome-description"
          >
            <div class="border-b border-primary/20 px-5 py-4 text-center">
              <div class="ll-eyebrow text-xs">Welcome to Legends Legacy</div>
              <h2 id="quest-welcome-title" class="mt-1 text-2xl text-primary [font-family:var(--ll-font-display)]">
                Your legend begins here
              </h2>
              <p id="quest-welcome-description" class="mx-auto mt-2 max-w-sm text-sm leading-6 text-[var(--ll-color-text-subtle)]">
                Choose your First Hunt, claim its guaranteed Essence, and follow the Tutorial quests to prepare for your
                journey through Shenic.
              </p>
            </div>
            <div class="space-y-4 px-5 py-4">
              <div class="grid gap-2 text-sm sm:grid-cols-2">
                <div class="ll-card flex items-start gap-2.5 px-3 py-2.5">
                  <span class="mt-0.5 text-primary" aria-hidden="true">✦</span>
                  <span>Compare three Essence ability pairs and choose your hunt.</span>
                </div>
                <div class="ll-card flex items-start gap-2.5 px-3 py-2.5">
                  <span class="mt-0.5 text-primary" aria-hidden="true">✦</span>
                  <span>Choose and equip a weapon from your Arms Chest, then enter Lumo Ruins.</span>
                </div>
              </div>
              @if (error(); as welcomeError) {
                <div class="ll-state ll-state-danger text-xs" role="alert">{{ welcomeError }}</div>
              }
            </div>
            <div class="flex justify-end border-t border-primary/20 px-5 py-4">
              <button
                type="button"
                class="ll-button ll-button-primary min-h-9 px-5 py-2 text-xs"
                [disabled]="loading() || welcomeStarting() || welcomeTransitioning()"
                (click)="beginTutorial()"
              >
                {{ welcomeStarting() ? 'Starting...' : 'Begin First Steps' }}
              </button>
            </div>
          </section>
        </div>
      }
    </ng-template>
  `,
})
export class QuestObjectiveGrimoireComponent extends QuestTrackerComponent {
  protected readonly open = signal(false);
  private readonly welcomeTpl = viewChild.required<TemplateRef<unknown>>('welcomeTpl');
  private readonly overlay = inject(Overlay);
  private readonly vcr = inject(ViewContainerRef);
  private welcomeRef: OverlayRef | null = null;

  protected summaryText(): string {
    const q = this.quest();
    if (!q) return '';
    if (this.readyToTurnIn()) return 'Ready to turn in';
    if (this.requiresChoice()) return q.choice?.selectionSummary ?? 'Choose';
    return this.objective()?.description || 'Open the Quest Journal';
  }

  private readonly routerRef = inject(Router);

  /** The panel's action: the old tracker's, and the Quest Journal when the objective has no destination. */
  protected act(): void {
    if (this.actionLabel() === 'Open Quests') void this.routerRef.navigateByUrl('/game/quests');
    else this.navigate();
  }

  protected actionLabel(): string | null {
    const q = this.quest();
    if (!q) return null;
    if (this.readyToTurnIn()) return 'Turn In Quest';
    if (this.requiresChoice()) return this.choiceActionLabel(q);
    return this.objective()?.presentation?.destinationRoute ? this.objective()?.presentation?.actionLabel || 'Go' : 'Open Quests';
  }

  // The welcome dialog lives at the document's root (a CDK overlay), outside the TopBar's containment.
  private readonly welcomeEffect = effect(() => {
    const show = this.welcomeOpen();
    untracked(() => {
      if (show && !this.welcomeRef) {
        this.welcomeRef = this.overlay.create({ positionStrategy: this.overlay.position().global() });
        this.welcomeRef.attach(new TemplatePortal(this.welcomeTpl(), this.vcr));
      } else if (!show && this.welcomeRef) {
        this.welcomeRef.dispose();
        this.welcomeRef = null;
      }
    });
  });

  private readonly cleanup = inject(DestroyRef).onDestroy(() => this.welcomeRef?.dispose());
}
