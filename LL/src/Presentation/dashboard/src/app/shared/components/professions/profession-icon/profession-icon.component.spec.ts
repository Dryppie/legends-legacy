import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfessionIconComponent } from './profession-icon.component';

describe('ProfessionIconComponent', () => {
  let component: ProfessionIconComponent;
  let fixture: ComponentFixture<ProfessionIconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfessionIconComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfessionIconComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
