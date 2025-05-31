import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MarketPlaceInventoryItemComponent } from './market-place-inventory-item.component';

describe('MarketPlaceInventoryItemComponent', () => {
  let component: MarketPlaceInventoryItemComponent;
  let fixture: ComponentFixture<MarketPlaceInventoryItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketPlaceInventoryItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MarketPlaceInventoryItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
