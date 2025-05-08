import { TestBed } from '@angular/core/testing';

import { CharacterManagerService } from './character-manager.service';

describe('CharacterManagerService', () => {
  let service: CharacterManagerService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(CharacterManagerService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
