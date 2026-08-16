import { TestBed } from '@angular/core/testing';

import { CharacterService } from './character.service';
import { httpTestingProviders } from '../../../../shared/testing/http-testing-providers';

describe('CharacterService', () => {
  let service: CharacterService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: httpTestingProviders });
    service = TestBed.inject(CharacterService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
