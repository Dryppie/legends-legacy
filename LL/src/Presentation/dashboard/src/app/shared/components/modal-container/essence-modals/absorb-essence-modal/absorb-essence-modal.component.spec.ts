import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AbsorbEssenceModalComponent } from './absorb-essence-modal.component';

describe('AbsorbEssenceModalComponent', () => {
  let component: AbsorbEssenceModalComponent;
  let fixture: ComponentFixture<AbsorbEssenceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AbsorbEssenceModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AbsorbEssenceModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
