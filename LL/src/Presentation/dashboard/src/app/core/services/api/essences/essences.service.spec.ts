import { TestBed } from '@angular/core/testing';

import { EssencesService } from './essences.service';
import { httpTestingProviders } from '../../../../shared/testing/http-testing-providers';

describe('EssencesService', () => {
  let service: EssencesService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(EssencesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
