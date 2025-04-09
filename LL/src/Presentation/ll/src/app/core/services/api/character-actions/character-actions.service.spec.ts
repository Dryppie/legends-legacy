import { TestBed } from '@angular/core/testing';

import { CharacterActionsService } from './character-actions.service';

describe('CharacterActionsService', () => {
  let service: CharacterActionsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CharacterActionsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
