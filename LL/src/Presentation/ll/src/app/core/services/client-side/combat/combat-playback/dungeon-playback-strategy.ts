import { Observable, from, mergeMap, timer, map } from 'rxjs';
import { CombatEvent } from '../../../../../shared/models/Dtos/combatEventDto';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import { CombatPlaybackStrategy } from './combat-playback-strategy';

export class DungeonPlaybackStrategy implements CombatPlaybackStrategy {
  stream(result: CombatResultDto, now: () => number): Observable<CombatEvent> {
    const start = new Date(result.startedAt).getTime();
    const elapsed = (now() - start) / 1000; // seconds since server started fight

    // Produce events in chronological order
    return from(result.eventLog).pipe(
      mergeMap((ev) => {
        const evTime = ev.timestamp / 10; // seconds since fight start
        const delayMs = Math.max(0, (evTime - elapsed) * 1000);
        return timer(delayMs).pipe(map(() => ev));
      }),
      //   finalize(() => this.onFinished(result)),
    );
  }

  //   private onFinished(result: CombatResultDto) {
  //     this.levelingService.gainExperience(result.experienceGained);
  //     this.combatLogService.add({
  //       outcome: result.outcome,
  //       xp: result.experienceGained,
  //     });
  //   }
}
