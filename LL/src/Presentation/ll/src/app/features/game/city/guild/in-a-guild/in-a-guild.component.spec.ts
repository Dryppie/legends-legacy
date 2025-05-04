import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InAGuildComponent } from './in-a-guild.component';

describe('InAGuildComponent', () => {
  let component: InAGuildComponent;
  let fixture: ComponentFixture<InAGuildComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InAGuildComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InAGuildComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
