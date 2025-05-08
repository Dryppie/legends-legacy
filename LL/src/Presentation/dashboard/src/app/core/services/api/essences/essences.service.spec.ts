import { TestBed } from '@angular/core/testing';

import { EssencesService } from './essences.service';

describe('EssencesService', () => {
  let service: EssencesService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EssencesService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
