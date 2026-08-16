import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ItemTooltipComponent } from './itemTooltip.component';
import { testItem } from '../../../testing/model-fixtures';

describe('TooltipComponent', () => {
  let component: ItemTooltipComponent;
  let fixture: ComponentFixture<ItemTooltipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ItemTooltipComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ItemTooltipComponent);
    component = fixture.componentInstance;
    component.item = testItem;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
