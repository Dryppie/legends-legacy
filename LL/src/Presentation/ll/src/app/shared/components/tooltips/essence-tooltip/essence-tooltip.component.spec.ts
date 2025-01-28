import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EssenceTooltipComponent } from './essence-tooltip.component';

describe('EssenceTooltipComponent', () => {
  let component: EssenceTooltipComponent;
  let fixture: ComponentFixture<EssenceTooltipComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EssenceTooltipComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EssenceTooltipComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
