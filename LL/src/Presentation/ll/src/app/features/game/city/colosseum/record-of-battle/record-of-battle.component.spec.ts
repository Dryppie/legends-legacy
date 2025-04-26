import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RecordOfBattleComponent } from './record-of-battle.component';

describe('RecordOfBattleComponent', () => {
  let component: RecordOfBattleComponent;
  let fixture: ComponentFixture<RecordOfBattleComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RecordOfBattleComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RecordOfBattleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
