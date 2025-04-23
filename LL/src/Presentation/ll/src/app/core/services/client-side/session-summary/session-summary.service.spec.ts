import { TestBed } from '@angular/core/testing';

import { SessionSummaryService } from './session-summary.service';

describe('SessionSummaryService', () => {
  let service: SessionSummaryService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(SessionSummaryService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
