import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EquippedEssencesComponent } from './equipped-essences.component';

describe('EquippedEssencesComponent', () => {
  let component: EquippedEssencesComponent;
  let fixture: ComponentFixture<EquippedEssencesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EquippedEssencesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EquippedEssencesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
