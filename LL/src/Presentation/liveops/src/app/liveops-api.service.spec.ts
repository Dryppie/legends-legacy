import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { LiveOpsApiService } from './liveops-api.service';

describe('LiveOpsApiService', () => {
  let service: LiveOpsApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LiveOpsApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LiveOpsApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the antiforgery token for mutations', async () => {
    const tokenPromise = service.initializeAntiforgery();
    http.expectOne('/auth/antiforgery').flush({ requestToken: 'xsrf-token' });
    await tokenPromise;

    const mutation = service.mute('character-1', {
      operationId: 'operation-1',
      reason: 'case-42',
    });
    const request = http.expectOne('/api/liveops/chat/characters/character-1/mutes');
    expect(request.request.headers.get('X-XSRF-TOKEN')).toBe('xsrf-token');
    request.flush({ isSuccess: true, data: null, errorMessage: '' });
    await mutation;
  });
});
