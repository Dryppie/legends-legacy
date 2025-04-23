import { TestBed } from '@angular/core/testing';

import { CombatLogService } from './combat-log.service';

describe('CombatLogService', () => {
  let service: CombatLogService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CombatLogService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
