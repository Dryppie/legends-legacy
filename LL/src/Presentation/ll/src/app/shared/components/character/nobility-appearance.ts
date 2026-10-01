import {
  DestroyRef,
  Signal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { Subscription, switchMap, timer } from 'rxjs';
import {
  NobilityAppearance,
  NobilityService,
} from '../../../core/services/api/nobility/nobility.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';

export interface NobilityAppearanceState {
  /** The membership while it lasts, whether or not the player shows it. */
  readonly active: Signal<NobilityAppearance | null>;
  /** The membership while it lasts and the player shows the Noble badge. */
  readonly visible: Signal<NobilityAppearance | null>;
}

/**
 * A character's Nobility: fetched now and every minute, and cleared the moment it expires. Call it in an injection
 * context (a field initializer); `characterId` may change, and an empty id clears it.
 */
export function injectNobilityAppearance(
  characterId: () => string | null | undefined,
): NobilityAppearanceState {
  const nobility = inject(NobilityService);
  const time = inject(TimeSyncService);
  const appearance = signal<NobilityAppearance | null>(null);
  const clock = signal(Date.now());
  let expiry: ReturnType<typeof setTimeout> | undefined;
  let updates: Subscription | undefined;

  const stop = () => {
    updates?.unsubscribe();
    updates = undefined;
    clearTimeout(expiry);
  };
  const watch = (id: string | null | undefined) => {
    stop();
    appearance.set(null);
    if (!id) return;
    updates = timer(0, 60_000)
      .pipe(switchMap(() => nobility.publicAppearance(id)))
      .subscribe((value) => {
        appearance.set(value);
        clock.set(time.now());
        clearTimeout(expiry);
        if (value.expiresAt) {
          const delay = Date.parse(value.expiresAt) - time.now();
          if (delay > 0) {
            expiry = setTimeout(
              () => appearance.set(null),
              Math.min(delay, 2_147_000_000),
            );
          }
        }
      });
  };

  effect(() => {
    const id = characterId();
    untracked(() => watch(id));
  });
  inject(DestroyRef).onDestroy(stop);

  const active = computed(() => {
    const value = appearance();
    return value?.expiresAt && Date.parse(value.expiresAt) > clock()
      ? value
      : null;
  });
  const visible = computed(() => {
    const value = active();
    return value?.showBadge ? value : null;
  });
  return { active, visible };
}
