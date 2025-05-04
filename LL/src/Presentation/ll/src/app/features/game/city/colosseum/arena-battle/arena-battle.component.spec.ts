import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ArenaBattleComponent } from './arena-battle.component';

describe('ArenaBattleComponent', () => {
  let component: ArenaBattleComponent;
  let fixture: ComponentFixture<ArenaBattleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ArenaBattleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ArenaBattleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
