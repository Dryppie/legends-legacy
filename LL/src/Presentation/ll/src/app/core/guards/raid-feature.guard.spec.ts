import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { environment } from '../../../environments/environment';
import { raidFeatureGuard } from './raid-feature.guard';

describe('raidFeatureGuard', () => {
  let router: jasmine.SpyObj<Router>;
  let raidsWereEnabled: boolean;

  beforeEach(() => {
    raidsWereEnabled = environment.features.raids;
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    TestBed.configureTestingModule({
      providers: [{ provide: Router, useValue: router }],
    });
  });

  afterEach(() => {
    environment.features.raids = raidsWereEnabled;
  });

  it('allows navigation when raids are enabled', () => {
    environment.features.raids = true;

    expect(runGuard()).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects to the world map when raids are disabled', () => {
    const worldTree = {} as UrlTree;
    environment.features.raids = false;
    router.createUrlTree.and.returnValue(worldTree);

    expect(runGuard()).toBe(worldTree);
    expect(router.createUrlTree).toHaveBeenCalledOnceWith(['/game/world']);
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() =>
      raidFeatureGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }
});
