import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RegularCraftingComponent } from './regular-crafting.component';

describe('RegularCraftingComponent', () => {
  let component: RegularCraftingComponent;
  let fixture: ComponentFixture<RegularCraftingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RegularCraftingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RegularCraftingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
