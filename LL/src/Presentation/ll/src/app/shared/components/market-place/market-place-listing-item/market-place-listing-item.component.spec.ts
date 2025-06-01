import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MarketPlaceListingItemComponent } from './market-place-listing-item.component';

describe('MarketPlaceListingItemComponent', () => {
  let component: MarketPlaceListingItemComponent;
  let fixture: ComponentFixture<MarketPlaceListingItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketPlaceListingItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MarketPlaceListingItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
