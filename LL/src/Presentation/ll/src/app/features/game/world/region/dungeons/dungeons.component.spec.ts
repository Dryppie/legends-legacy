import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { DungeonStateService } from '../../../../../core/services/api/dungeon/dungeon-state.service';
import { DungeonsComponent } from './dungeons.component';
import { TestBed } from '@angular/core/testing';
import { DungeonPreviewData } from '../../../../../shared/models/Dtos/dungeons/dungeonPreviewData';

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

  it('tracks refreshed dungeon previews by family so card state is retained', () => {
    const component = Object.create(
      DungeonsComponent.prototype,
    ) as DungeonsComponent;
    const preview = {
      id: 'goblin_mines_ii',
      familyId: 'goblin_mines',
    } as DungeonPreviewData;

    expect(component.trackDungeon(0, preview)).toBe('goblin_mines');
  });
});
