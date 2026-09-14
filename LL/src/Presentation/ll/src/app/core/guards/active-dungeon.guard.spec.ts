import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { firstValueFrom, Observable, of, throwError } from 'rxjs';
import {
  DungeonRun,
  DungeonService,
} from '../services/api/dungeon/dungeon.service';
import { activeDungeonGuard } from './active-dungeon.guard';

describe('activeDungeonGuard', () => {
  let dungeons: jasmine.SpyObj<DungeonService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    dungeons = jasmine.createSpyObj<DungeonService>('DungeonService', [
      'getActiveDungeon',
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: DungeonService, useValue: dungeons },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('allows navigation when a dungeon run exists', async () => {
    dungeons.getActiveDungeon.and.returnValue(of({} as DungeonRun));

    expect(await runGuard()).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects to the world map when no dungeon run exists', async () => {
    const worldTree = {} as UrlTree;
    dungeons.getActiveDungeon.and.returnValue(of(null));
    router.createUrlTree.and.returnValue(worldTree);

    expect(await runGuard()).toBe(worldTree);
    expect(router.createUrlTree).toHaveBeenCalledOnceWith(['/game/world']);
  });

  it('allows the dungeon page to handle active-run lookup errors', async () => {
    dungeons.getActiveDungeon.and.returnValue(
      throwError(() => new Error('Unavailable')),
    );

    expect(await runGuard()).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  function runGuard(): Promise<boolean | UrlTree> {
    const result = TestBed.runInInjectionContext(() =>
      activeDungeonGuard(
        {} as ActivatedRouteSnapshot,
        {} as RouterStateSnapshot,
      ),
    );

    return firstValueFrom(result as Observable<boolean | UrlTree>);
  }
});
