import { TestBed } from '@angular/core/testing';

import { CreatureService } from './creature.service';
import { httpTestingProviders } from '../../../../shared/testing/http-testing-providers';

describe('CreatureService', () => {
  let service: CreatureService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(CreatureService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
