import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiService } from './api.service';

describe('ApiService versioned mutations', () => {
  let api: ApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  for (const method of ['put', 'patch', 'delete'] as const) {
    it(`exposes domain versions for ${method.toUpperCase()}`, () => {
      let result: unknown;
      const request = { value: 1 };

      api[`${method}Versioned`]<{ updated: boolean }>(
        'resource/1',
        request,
      ).subscribe((response) => (result = response));

      const pending = http.expectOne(`${api.apiUrl}resource/1`);
      expect(pending.request.method).toBe(method.toUpperCase());
      pending.flush(
        { updated: true },
        { headers: { 'X-LL-Domain-Versions': '{"inventory":7}' } },
      );

      expect(result).toEqual({
        data: { updated: true },
        domainVersions: { inventory: 7 },
      });
    });
  }
});
