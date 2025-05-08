import { TestBed } from '@angular/core/testing';
import { CombatStateService } from './combat-state.service';

describe('CombatStateService', () => {
  let service: CombatStateService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CombatStateService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
