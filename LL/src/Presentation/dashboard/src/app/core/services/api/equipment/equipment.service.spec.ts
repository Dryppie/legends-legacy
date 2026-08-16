import { TestBed } from '@angular/core/testing';

import { EquipmentService } from './equipment.service';
import { httpTestingProviders } from '../../../../shared/testing/http-testing-providers';

describe('EquipmentService', () => {
  let service: EquipmentService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(EquipmentService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
