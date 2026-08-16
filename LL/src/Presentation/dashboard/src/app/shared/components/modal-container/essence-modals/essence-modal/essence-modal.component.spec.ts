import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EssenceModalComponent } from './essence-modal.component';
import { testEssence } from '../../../../testing/model-fixtures';

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
    component.essence = testEssence;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
