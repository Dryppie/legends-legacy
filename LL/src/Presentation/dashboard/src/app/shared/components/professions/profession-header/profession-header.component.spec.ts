import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProfessionHeaderComponent } from './profession-header.component';

describe('ProfessionHeaderComponent', () => {
  let component: ProfessionHeaderComponent;
  let fixture: ComponentFixture<ProfessionHeaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProfessionHeaderComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ProfessionHeaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
