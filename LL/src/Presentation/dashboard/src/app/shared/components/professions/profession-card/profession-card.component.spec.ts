import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfessionCardComponent } from './profession-card.component';

describe('ProfessionCardComponent', () => {
  let component: ProfessionCardComponent;
  let fixture: ComponentFixture<ProfessionCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfessionCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfessionCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
