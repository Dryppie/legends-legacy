import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AbilityTooltipComponent } from './ability-tooltip.component';

describe('AbilityTooltipComponent', () => {
  let component: AbilityTooltipComponent;
  let fixture: ComponentFixture<AbilityTooltipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AbilityTooltipComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AbilityTooltipComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
