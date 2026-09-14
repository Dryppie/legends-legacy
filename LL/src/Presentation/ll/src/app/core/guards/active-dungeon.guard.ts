import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { DungeonService } from '../services/api/dungeon/dungeon.service';

export const activeDungeonGuard: CanActivateFn = () => {
  const dungeons = inject(DungeonService);
  const router = inject(Router);

  return dungeons.getActiveDungeon().pipe(
    map((activeDungeon) =>
      activeDungeon ? true : router.createUrlTree(['/game/world']),
    ),
    catchError(() => of(true)),
  );
};
