import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ChampionsMarketComponent } from './champions-market.component';

describe('ChampionsMarketComponent', () => {
  let component: ChampionsMarketComponent;
  let fixture: ComponentFixture<ChampionsMarketComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChampionsMarketComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ChampionsMarketComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
