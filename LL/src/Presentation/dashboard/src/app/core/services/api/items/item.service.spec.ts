import { TestBed } from '@angular/core/testing';

import { ItemService } from './item.service';
import { httpTestingProviders } from '../../../../shared/testing/http-testing-providers';

describe('ItemService', () => {
  let service: ItemService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(ItemService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
