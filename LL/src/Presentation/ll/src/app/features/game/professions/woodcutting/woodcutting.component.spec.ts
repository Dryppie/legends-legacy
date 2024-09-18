import { ComponentFixture, TestBed } from '@angular/core/testing';

import { WoodcuttingComponent } from './woodcutting.component';

describe('WoodcuttingComponent', () => {
  let component: WoodcuttingComponent;
  let fixture: ComponentFixture<WoodcuttingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [WoodcuttingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(WoodcuttingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
