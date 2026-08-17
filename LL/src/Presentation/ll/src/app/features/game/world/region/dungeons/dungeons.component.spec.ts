import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { DungeonStateService } from '../../../../../core/services/api/dungeon/dungeon-state.service';
import { DungeonsComponent } from './dungeons.component';
import { TestBed } from '@angular/core/testing';

describe('DungeonsComponent', () => {
  it('refreshes dungeon availability whenever the page is opened', () => {
    const dungeonState = jasmine.createSpyObj<DungeonStateService>(
      'DungeonStateService',
      ['refresh'],
    );
    const characterState = jasmine.createSpyObj<CharacterStateService>(
      'CharacterStateService',
      ['refreshIfDirty'],
    );

    const component = TestBed.runInInjectionContext(
      () => new DungeonsComponent(dungeonState, characterState),
    );
    component.ngOnInit();

    expect(dungeonState.refresh).toHaveBeenCalledOnceWith();
  });
});
