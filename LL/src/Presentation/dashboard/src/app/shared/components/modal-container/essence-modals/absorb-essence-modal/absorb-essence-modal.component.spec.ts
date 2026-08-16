import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AbsorbEssenceModalComponent } from './absorb-essence-modal.component';
import { httpTestingProviders } from '../../../../testing/http-testing-providers';

describe('AbsorbEssenceModalComponent', () => {
  let component: AbsorbEssenceModalComponent;
  let fixture: ComponentFixture<AbsorbEssenceModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AbsorbEssenceModalComponent],
      providers: httpTestingProviders,
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
