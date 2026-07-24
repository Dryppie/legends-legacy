import {
  Component,
  effect,
  ElementRef,
  EventEmitter,
  OnDestroy,
  Output,
  ViewChild,
} from '@angular/core';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';

@Component({
    selector: 'app-progress-bar',
    imports: [],
    templateUrl: './progress-bar.component.html'
})
export class ProgressBarComponent implements OnDestroy {
  @ViewChild('progressBar', { static: true })
  progressBar!: ElementRef<HTMLDivElement>;
  @Output() remainingTimeChange = new EventEmitter<string>();

  private animationFrameId: number = 0;
  private actionSubscription: Subscription | null = null;
  private readonly craftingActionDurationSeconds = 10;

  constructor(
    private readonly state: CharacterActionsStateService,
    private readonly timeSync: TimeSyncService,
  ) {
    effect(() => {
      const action = this.state.currentAction();
      if (action) {
        this.startProgressBar(action);
      } else {
        this.stopProgressBar();
      }
    });
  }

  ngOnDestroy(): void {
    this.stopProgressBar();
    if (this.actionSubscription) {
      this.actionSubscription.unsubscribe();
    }
  }
  private startProgressBar(action: CharacterActionDto): void {
    this.cancelAnimation();
    const progressBarElement = this.progressBar.nativeElement;
    let duration = environment.baseDuration;
    let startTime: number;
    const now = this.timeSync.now();

    if (
      action.characterActionType === CharacterActionType.Combat ||
      action.isDeleted
    ) {
      // The server deadline is canonical. Deriving the start from it means a late
      // response advances the same bar instead of restarting it from zero.
      const deadline = new Date(
        action.nextResolutionAt ?? action.updatedAt,
      ).getTime();
      startTime = deadline - duration * 1000;
    } else if (action.characterActionType === CharacterActionType.Crafting) {
      // Crafting: updatedAt is in the past, meaning the current tempering tick started before now.
      const actionUpdatedAt = new Date(action.updatedAt).getTime();
      startTime = actionUpdatedAt; // The action started in the past
      duration = this.craftingActionDurationSeconds;
    } else {
      const actionUpdatedAt = new Date(action.updatedAt).getTime();
      startTime = actionUpdatedAt;
      duration = environment.baseDuration;
    }

    // Calculate initial progress
    const elapsedTime = (now - startTime) / 1000;
    const initialProgress = Math.max(
      0,
      Math.min((elapsedTime / duration) * 100, 100),
    );

    // Set initial progress bar width
    progressBarElement.style.width = `${initialProgress}%`;

    const updateProgress = () => {
      const elapsed = (this.timeSync.now() - startTime) / 1000;
      const progress = Math.max(0, Math.min((elapsed / duration) * 100, 100));

      progressBarElement.style.width = `${progress}%`;

      const remainingSeconds = Math.max(duration - Math.floor(elapsed), 0);
      this.remainingTimeChange.emit(this.formatTime(remainingSeconds));

      if (progress < 100) {
        this.animationFrameId = requestAnimationFrame(updateProgress);
      }
    };

    this.animationFrameId = requestAnimationFrame(updateProgress);
  }

  private formatTime(seconds: number): string {
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = Math.floor(seconds % 60);
    return `${this.padTime(minutes)}:${this.padTime(remainingSeconds)}`;
  }

  private padTime(value: number): string {
    return value < 10 ? `0${value}` : `${value}`;
  }

  private stopProgressBar(): void {
    this.cancelAnimation();
    this.progressBar.nativeElement.style.width = '0%';
  }

  private cancelAnimation(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = 0;
    }
  }
}
