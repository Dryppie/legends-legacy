import { TestBed } from '@angular/core/testing';

import { ColosseumService } from './colosseum.service';

describe('ColosseumService', () => {
  let service: ColosseumService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(ColosseumService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
