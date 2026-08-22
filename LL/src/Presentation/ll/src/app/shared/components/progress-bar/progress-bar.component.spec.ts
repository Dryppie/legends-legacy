import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CharacterActionsStateService } from '../../../core/services/api/character-actions/character-actions.state.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';
import { CharacterActionDto } from '../../models/Dtos/characterActionDto';
import { CharacterActionType } from '../../models/enums/characterActionType';
import { ProgressBarComponent } from './progress-bar.component';

describe('ProgressBarComponent', () => {
  let fixture: ComponentFixture<ProgressBarComponent>;
  let currentAction: ReturnType<typeof signal<CharacterActionDto | null>>;
  let now: jasmine.Spy;

  beforeEach(async () => {
    currentAction = signal<CharacterActionDto | null>(null);
    now = jasmine.createSpy('now').and.returnValue(5_000);

    spyOn(globalThis, 'requestAnimationFrame').and.returnValue(1);
    spyOn(globalThis, 'cancelAnimationFrame');

    await TestBed.configureTestingModule({
      imports: [ProgressBarComponent],
      providers: [
        {
          provide: CharacterActionsStateService,
          useValue: { currentAction },
        },
        {
          provide: TimeSyncService,
          useValue: { now },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ProgressBarComponent);
  });

  it('keeps filling toward the switch unlock after combat is stopped', () => {
    let remainingTime = '';
    fixture.componentInstance.remainingTimeChange.subscribe(
      (value) => (remainingTime = value),
    );
    currentAction.set({
      characterActionType: CharacterActionType.Idle,
      lootTableId: '',
      updatedAt: new Date(4_000),
      blockedUntilUtc: new Date(10_000),
      resolutionIntervalMs: 10_000,
      revision: 'refreshed-stopped-combat',
      isDeleted: true,
    });

    fixture.detectChanges();

    const progress = fixture.nativeElement.querySelector(
      '[class*="bg-primary"]',
    ) as HTMLDivElement;
    expect(progress.style.width).toBe('50%');
    expect(remainingTime).toBe('00:05');
    expect(requestAnimationFrame).toHaveBeenCalled();
  });

  it('resets a deleted action that has no remaining switch lock', () => {
    currentAction.set({
      characterActionType: CharacterActionType.Idle,
      lootTableId: '',
      updatedAt: new Date(5_000),
      resolutionIntervalMs: 10_000,
      revision: 'stopped-action',
      isDeleted: true,
    });

    fixture.detectChanges();

    const progress = fixture.nativeElement.querySelector(
      '[class*="bg-primary"]',
    ) as HTMLDivElement;
    expect(progress.style.width).toBe('0%');
    expect(requestAnimationFrame).not.toHaveBeenCalled();
  });
});
