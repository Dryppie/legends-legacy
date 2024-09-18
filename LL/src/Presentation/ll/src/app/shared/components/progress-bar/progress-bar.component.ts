import {
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { CharacterActionsService } from '../../../core/services/character-actions/character-actions.service';
import { CharacterActionDto } from '../../models/characterActionDto';
import { Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-progress-bar',
  standalone: true,
  imports: [],
  templateUrl: './progress-bar.component.html',
  styleUrl: './progress-bar.component.css',
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
    const duration = environment.baseDuration; // Duration in seconds

    // Calculate how much time has passed since the action was last updated
    const actionUpdatedAt = new Date(action.updatedAt).getTime();
    const timeSinceUpdate = (Date.now() - actionUpdatedAt) / 1000; // Convert to seconds
    const initialProgress = Math.min((timeSinceUpdate / duration) * 100, 100); // Calculate initial progress based on the time elapsed since update

    // Set the progress bar's initial width based on time since update
    progressBarElement.style.width = `${initialProgress}%`;

    let startTime = Date.now() - timeSinceUpdate * 1000; // Adjust start time to account for elapsed time

    const updateProgress = () => {
      const elapsed = (Date.now() - startTime) / 1000; // Calculate elapsed time since the adjusted start time
      const progress = Math.min((elapsed / duration) * 100, 100);

      progressBarElement.style.width = `${progress}%`;

      const remainingSeconds = Math.max(duration - Math.floor(elapsed), 1);
      this.remainingTimeChange.emit(this.formatTime(remainingSeconds));

      if (progress < 100) {
        this.animationFrameId = requestAnimationFrame(updateProgress);
      } else {
        startTime = Date.now(); // Reset start time if needed for future progress
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
