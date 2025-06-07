import {
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { CharacterActionsService } from '../../../core/services/api/character-actions/character-actions.service';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CharacterActionType } from '../../models/enums/characterActionType';

@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [],
  templateUrl: './progress-bar.component.html',
})
export class ProgressBarComponent implements OnInit, OnDestroy {
  @ViewChild('progressBar', { static: true })
  progressBar!: ElementRef<HTMLDivElement>;
  @Output() remainingTimeChange = new EventEmitter<string>();

  private animationFrameId: number = 0;
  private actionSubscription: Subscription | null = null;

  constructor(private characterActionsService: CharacterActionsService) {}

  ngOnInit(): void {
    this.actionSubscription =
      this.characterActionsService.currentAction$.subscribe((action) => {
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
    const progressBarElement = this.progressBar.nativeElement;
    let duration = environment.baseDuration;
    let startTime: number;

    if (
      (action.characterActionType === CharacterActionType.Combat ||
        action.isDeleted) &&
      new Date(action.updatedAt).getTime() > Date.now()
    ) {
      // Combat: updatedAt is in the future, meaning time left is from now until then
      const updatedAt = new Date(action.updatedAt).getTime();
      const timeUntilFinished = (updatedAt - Date.now()) / 1000; // Remaining time
      duration = Math.max(timeUntilFinished, 0); // Ensure non-negative duration
      startTime = Date.now(); // Start now, since the fight is ongoing
    } else {
      // Non-combat: updatedAt is in the past, meaning it started before and has 6 seconds duration
      const actionUpdatedAt = new Date(action.updatedAt).getTime();
      startTime = actionUpdatedAt; // The action started in the past
      duration = 6; // Fixed duration of 6 seconds
    }

    // Calculate initial progress
    const elapsedTime = (Date.now() - startTime) / 1000;
    const initialProgress = Math.min((elapsedTime / duration) * 100, 100);

    // Set initial progress bar width
    progressBarElement.style.width = `${initialProgress}%`;

    const updateProgress = () => {
      const elapsed = (Date.now() - startTime) / 1000; // Elapsed time since action started
      const progress = Math.min((elapsed / duration) * 100, 100);

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
    if (this.animationFrameId) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = 0;
    }
    // Reset progress bar width if needed
    this.progressBar.nativeElement.style.width = '0%';
  }
}
