import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ItemTooltipComponent } from './itemTooltip.component';

describe('TooltipComponent', () => {
  let component: ItemTooltipComponent;
  let fixture: ComponentFixture<ItemTooltipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ItemTooltipComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ItemTooltipComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
