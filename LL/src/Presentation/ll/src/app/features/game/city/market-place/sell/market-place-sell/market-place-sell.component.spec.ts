import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MarketPlaceSellComponent } from './market-place-sell.component';

describe('MarketPlaceSellComponent', () => {
  let component: MarketPlaceSellComponent;
  let fixture: ComponentFixture<MarketPlaceSellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketPlaceSellComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MarketPlaceSellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
