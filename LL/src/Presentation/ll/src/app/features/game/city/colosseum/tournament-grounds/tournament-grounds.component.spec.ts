import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TournamentGroundsComponent } from './tournament-grounds.component';

describe('TournamentGroundsComponent', () => {
  let component: TournamentGroundsComponent;
  let fixture: ComponentFixture<TournamentGroundsComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TournamentGroundsComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TournamentGroundsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
