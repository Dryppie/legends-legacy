import { TestBed } from '@angular/core/testing';

import { ApiService } from './api.service';
import { httpTestingProviders } from '../../../shared/testing/http-testing-providers';

describe('ApiService', () => {
  let service: ApiService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(ApiService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
