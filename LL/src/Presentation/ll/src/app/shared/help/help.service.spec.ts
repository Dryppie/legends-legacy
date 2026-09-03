import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { HelpService } from './help.service';
describe('HelpService', () => {
  it('loads and caches the current guide directly', () => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    const service = TestBed.inject(HelpService), http = TestBed.inject(HttpTestingController);
    const guide = { title: 'Inventory', lastReviewed: '', sections: [] };
    service.loadGuide('inventory').subscribe(value => expect(value).toEqual(guide));
    http.expectOne('assets/help/guides/inventory.json').flush(guide);
    service.loadGuide('inventory').subscribe(value => expect(value).toEqual(guide));
    http.verify();
  });
});
