import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RemoveEssenceModalComponent } from './remove-essence-modal.component';

describe('RemoveEssenceModalComponent', () => {
  let component: RemoveEssenceModalComponent;
  let fixture: ComponentFixture<RemoveEssenceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RemoveEssenceModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RemoveEssenceModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
