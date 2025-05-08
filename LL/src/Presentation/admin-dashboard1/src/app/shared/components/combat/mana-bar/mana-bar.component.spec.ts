import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ManaBarComponent } from './mana-bar.component';

describe('ManaBarComponent', () => {
  let component: ManaBarComponent;
  let fixture: ComponentFixture<ManaBarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ManaBarComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ManaBarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
