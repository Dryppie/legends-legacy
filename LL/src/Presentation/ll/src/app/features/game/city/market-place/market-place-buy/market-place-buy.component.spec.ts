import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MarketPlaceBuyComponent } from './market-place-buy.component';

describe('MarketPlaceBuyComponent', () => {
  let component: MarketPlaceBuyComponent;
  let fixture: ComponentFixture<MarketPlaceBuyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MarketPlaceBuyComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(MarketPlaceBuyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
