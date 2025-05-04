import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ColosseumBattleComponent } from './colosseum-battle.component';

describe('ColosseumBattleComponent', () => {
  let component: ColosseumBattleComponent;
  let fixture: ComponentFixture<ColosseumBattleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ColosseumBattleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ColosseumBattleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
