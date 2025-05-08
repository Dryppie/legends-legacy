import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfessionActionComponent } from './profession-action.component';

describe('ProfessionActionComponent', () => {
  let component: ProfessionActionComponent;
  let fixture: ComponentFixture<ProfessionActionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfessionActionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfessionActionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
