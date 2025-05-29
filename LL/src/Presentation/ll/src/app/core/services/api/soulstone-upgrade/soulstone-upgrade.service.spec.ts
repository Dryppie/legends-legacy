import { TestBed } from '@angular/core/testing';

import { SoulstoneUpgradeService } from './soulstone-upgrade.service';

describe('SoulstoneUpgradeService', () => {
  let service: SoulstoneUpgradeService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SoulstoneUpgradeService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
