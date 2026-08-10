import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { DungeonStateService } from '../../../../../core/services/api/dungeon/dungeon-state.service';
import { DungeonsComponent } from './dungeons.component';

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

    const component = new DungeonsComponent(dungeonState, characterState);
    component.ngOnInit();

    expect(dungeonState.refresh).toHaveBeenCalledOnceWith();
  });
});
