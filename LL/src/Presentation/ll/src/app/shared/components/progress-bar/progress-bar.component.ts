import {
  Component,
  effect,
  ElementRef,
  EventEmitter,
  Input,
  OnDestroy,
  Output,
  ViewChild,
} from '@angular/core';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';

@Component({
  selector: 'app-progress-bar',
  imports: [],
  templateUrl: './progress-bar.component.html',
})
export class ProgressBarComponent implements OnDestroy {
  @ViewChild('progressBar', { static: true })
  progressBar!: ElementRef<HTMLDivElement>;
  @Output() remainingTimeChange = new EventEmitter<string>();
  @Input() vertical = false;

  private animationFrameId: number = 0;
  private actionSubscription: Subscription | null = null;

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
    const durationMs = action.resolutionIntervalMs;
    const deadlineValue = action.nextResolutionAtUtc ?? action.nextResolutionAt;
    if (action.isDeleted || !durationMs || durationMs <= 0 || !deadlineValue) {
      this.stopProgressBar();
      return;
    }

    const deadline = new Date(deadlineValue).getTime();
    if (!Number.isFinite(deadline)) {
      this.stopProgressBar();
      return;
    }

    const duration = durationMs / 1000;
    const startTime = deadline - durationMs;
    const now = this.timeSync.now();

    // Calculate initial progress
    const elapsedTime = (now - startTime) / 1000;
    const initialProgress = Math.max(
      0,
      Math.min((elapsedTime / duration) * 100, 100),
    );

    this.setProgress(progressBarElement, initialProgress);

    const updateProgress = () => {
      const elapsed = (this.timeSync.now() - startTime) / 1000;
      const progress = Math.max(0, Math.min((elapsed / duration) * 100, 100));

      this.setProgress(progressBarElement, progress);

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
    this.setProgress(this.progressBar.nativeElement, 0);
  }

  private setProgress(element: HTMLDivElement, progress: number): void {
    if (this.vertical) {
      element.style.width = '100%';
      element.style.height = '100%';
      element.style.transform = `scaleY(${progress / 100})`;
      return;
    }

    element.style.transform = '';
    element.style.width = `${progress}%`;
    element.style.height = '100%';
  }

  private cancelAnimation(): void {
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = 0;
    }
  }
}
