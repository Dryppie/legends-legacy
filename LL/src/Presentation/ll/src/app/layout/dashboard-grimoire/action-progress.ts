import { DestroyRef, Signal, effect, inject, signal } from '@angular/core';
import { CharacterActionsStateService } from '../../core/services/api/character-actions/character-actions.state.service';
import { TimeSyncService } from '../../core/services/api/time-sync/time-sync.service';
import { CharacterActionDto } from '../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';

export interface ActionProgress {
  /** 0–1 through the current resolution interval. */
  progress: Signal<number>;
  /** Time left in the interval, "mm:ss". */
  remaining: Signal<string>;
}

/** What the rail's Activity says the character is doing (the words of the old sidebar's current action). */
export function actionLabel(action: CharacterActionDto | null, now = Date.now()): string {
  if (!action) return 'Idle';
  const deadline = new Date(
    (action.isDeleted ? action.blockedUntilUtc : null) ?? action.nextResolutionAtUtc ?? action.updatedAt,
  ).getTime();
  if (action.isDeleted && deadline > now) return 'Combat ending - recovery';
  return action.characterActionType === CharacterActionType.Combat ? 'Engaged in Combat' : 'Idle';
}

/** Progress through an interval that ends at `deadline` and lasts `durationMs`, at time `now`. */
export function intervalProgress(deadline: number, durationMs: number, now: number): { progress: number; remaining: string } {
  const elapsed = (now - (deadline - durationMs)) / 1000;
  const duration = durationMs / 1000;
  const progress = Math.max(0, Math.min(elapsed / duration, 1));
  const left = Math.max(duration - Math.floor(elapsed), 0);
  const pad = (n: number) => (n < 10 ? `0${n}` : `${n}`);
  return { progress, remaining: `${pad(Math.floor(left / 60))}:${pad(Math.floor(left % 60))}` };
}

/**
 * The current action's progress and time left, following the server clock frame by frame — the same reckoning as the
 * old sidebar's progress bar, as signals for the Grimoire Activity. Call in an injection context.
 */
export function injectActionProgress(): ActionProgress {
  const state = inject(CharacterActionsStateService);
  const timeSync = inject(TimeSyncService);
  const progress = signal(0);
  const remaining = signal('00:00');
  let frame = 0;
  const stop = () => {
    if (frame) cancelAnimationFrame(frame);
    frame = 0;
  };

  effect(() => {
    const action = state.currentAction();
    stop();
    const durationMs = action?.resolutionIntervalMs ?? 0;
    const deadlineValue = action?.isDeleted ? action.blockedUntilUtc : action?.nextResolutionAtUtc;
    const deadline = deadlineValue ? new Date(deadlineValue).getTime() : Number.NaN;
    if (!action || durationMs <= 0 || !Number.isFinite(deadline)) {
      progress.set(0);
      remaining.set('00:00');
      return;
    }
    const tick = () => {
      const now = intervalProgress(deadline, durationMs, timeSync.now());
      progress.set(now.progress);
      remaining.set(now.remaining);
      frame = now.progress < 1 ? requestAnimationFrame(tick) : 0;
    };
    tick();
  });
  inject(DestroyRef).onDestroy(stop);
  return { progress: progress.asReadonly(), remaining: remaining.asReadonly() };
}
