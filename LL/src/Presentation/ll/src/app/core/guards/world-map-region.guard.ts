import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { CharacterActionsStateService } from '../services/api/character-actions/character-actions.state.service';
import { GameBootstrapStateService } from '../services/api/game-bootstrap/game-bootstrap-state.service';
import { RegionService } from '../services/client-side/region/region.service';
import { CharacterActionDto } from '../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';

export function getWorldMapRegionId(
  action: CharacterActionDto | null,
  regions: RegionService,
): string {
  const activeCombatAreaId =
    action &&
    !action.isDeleted &&
    action.characterActionType === CharacterActionType.Combat
      ? action.combatActionDetails?.area?.id
      : null;
  const areaId = activeCombatAreaId ?? action?.returnToCombatAreaId;

  return (
    (areaId ? regions.getRegionIdByAreaId(areaId) : null) ??
    regions.getFirstRegionId()
  );
}

export const worldMapRegionGuard: CanMatchFn = () => {
  const bootstrap = inject(GameBootstrapStateService);
  const actions = inject(CharacterActionsStateService);
  const regions = inject(RegionService);
  const router = inject(Router);
  const regionUrl = () =>
    router.createUrlTree([
      '/game/world',
      getWorldMapRegionId(actions.currentAction(), regions),
    ]);

  return bootstrap.load().pipe(
    map(() => regionUrl()),
    catchError(() => of(regionUrl())),
  );
};
