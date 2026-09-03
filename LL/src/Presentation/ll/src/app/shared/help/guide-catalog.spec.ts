import { Route, Routes } from '@angular/router';
import { DASHBOARD_ROUTES } from '../../layout/dashboard/dashboard.routes';
import { CHARACTER_ROUTES } from '../../features/game/character/character.routes';
import { CITY_ROUTES } from '../../features/game/city/city.routes';
import { WORLD_ROUTES } from '../../features/game/world/world.routes';
import { PROPHECIES_ROUTES } from '../../features/game/prophecies/prophecies.routes';
import { SETTINGS_ROUTES } from '../../features/game/settings/settings.routes';
import { ALL_GUIDE_PAGE_IDS } from './guide-catalog';

describe('guide route catalog', () => {
  const routeSets: Routes[] = [
    DASHBOARD_ROUTES,
    CHARACTER_ROUTES,
    CITY_ROUTES,
    WORLD_ROUTES,
    PROPHECIES_ROUTES,
    SETTINGS_ROUTES,
  ];

  it('marks every concrete game page as guided or intentionally guide-free', () => {
    const pages = routeSets.flatMap((routes) => concretePages(routes));
    const invalidGuideConfiguration = pages
      .filter((route) => {
        const hasGuide = typeof route.data?.['guidePageId'] === 'string';
        const guideDisabled = route.data?.['guideDisabled'] === true;
        return hasGuide === guideDisabled;
      })
      .map((route) => route.path);

    expect(invalidGuideConfiguration).toEqual([]);
  });

  it('uses every catalog entry in route metadata', () => {
    const usedIds = new Set(
      routeSets
        .flatMap((routes) => concretePages(routes))
        .map((route) => route.data?.['guidePageId'])
        .filter((id): id is string => typeof id === 'string'),
    );

    expect(ALL_GUIDE_PAGE_IDS.filter((id) => !usedIds.has(id))).toEqual([]);
  });
});

function concretePages(routes: Routes): Route[] {
  return routes.flatMap((route) => {
    if (route.children?.length) return concretePages(route.children);
    if (
      route.redirectTo !== undefined ||
      (route.component === undefined && route.loadComponent === undefined)
    ) {
      return [];
    }
    return [route];
  });
}
