import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { environment } from '../../../environments/environment';
import { maintenanceGuard } from './maintenance.guard';

describe('maintenanceGuard', () => {
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    TestBed.configureTestingModule({
      providers: [{ provide: Router, useValue: router }],
    });
  });

  afterEach(() => {
    environment.maintenance.enabled = false;
  });

  it('allows navigation when maintenance is disabled', () => {
    environment.maintenance.enabled = false;

    const result = runGuard();

    expect(result).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects navigation to login when maintenance is enabled', () => {
    const loginTree = {} as UrlTree;
    environment.maintenance.enabled = true;
    router.createUrlTree.and.returnValue(loginTree);

    const result = runGuard();

    expect(result).toBe(loginTree);
    expect(router.createUrlTree).toHaveBeenCalledOnceWith(['/login']);
  });

  function runGuard() {
    return TestBed.runInInjectionContext(() =>
      maintenanceGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }
});
