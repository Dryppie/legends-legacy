import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EssenceModalComponent } from './essence-modal.component';

describe('EssenceModalComponent', () => {
  let component: EssenceModalComponent;
  let fixture: ComponentFixture<EssenceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EssenceModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EssenceModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
