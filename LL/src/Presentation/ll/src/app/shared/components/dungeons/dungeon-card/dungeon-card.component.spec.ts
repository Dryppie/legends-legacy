import { Router } from '@angular/router';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonPreviewData,
  DungeonRecord,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { DungeonCardComponent } from './dungeon-card.component';

describe('DungeonCardComponent difficulty preselection', () => {
  function createComponent(): DungeonCardComponent {
    return new DungeonCardComponent(
      {} as DungeonStateService,
      {} as CharacterStateService,
      {} as Router,
    );
  }

  function cleared(lastClearedAt: string): DungeonRecord {
    return {
      hasCleared: true,
      firstClearedAt: lastClearedAt,
      lastClearedAt,
      totalClears: 1,
    };
  }

  function createPreview(
    records: Partial<Record<DungeonDifficulty, DungeonRecord>>,
    unlocked: DungeonDifficulty[] = [
      DungeonDifficulty.Normal,
      DungeonDifficulty.Heroic,
      DungeonDifficulty.Mythic,
    ],
  ): DungeonPreviewData {
    const variants: Partial<Record<DungeonDifficulty, DungeonPreviewData>> = {};
    for (const difficulty of unlocked) {
      variants[difficulty] = {
        id: `goblin_mines_${difficulty}`,
        familyId: 'goblin_mines',
        region: 1,
        number: 1,
        title: 'Goblin Mines',
        difficulty,
        lore: '',
        requiredLevel: 5,
        rewards: [],
        unlockedDifficulties: unlocked,
        record: records[difficulty],
      };
    }

    return {
      ...(variants[DungeonDifficulty.Normal] as DungeonPreviewData),
      id: 'goblin_mines_Normal',
      unlockedDifficulties: unlocked,
      difficultyVariants: variants,
    };
  }

  it('defaults to the lowest unlocked difficulty when nothing has been cleared', () => {
    const component = createComponent();
    component.previewData = createPreview({});

    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Normal);
  });

  it('preselects the most recently cleared difficulty', () => {
    const component = createComponent();
    component.previewData = createPreview({
      [DungeonDifficulty.Normal]: cleared('2026-08-01T10:00:00Z'),
      [DungeonDifficulty.Heroic]: cleared('2026-08-05T10:00:00Z'),
    });

    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Heroic);
  });

  it('prefers the hardest cleared difficulty when clear times match', () => {
    const component = createComponent();
    const clearedAt = '2026-08-05T10:00:00Z';
    component.previewData = createPreview({
      [DungeonDifficulty.Normal]: cleared(clearedAt),
      [DungeonDifficulty.Mythic]: cleared(clearedAt),
    });

    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Mythic);
  });

  it('ignores difficulties that are no longer unlocked', () => {
    const component = createComponent();
    component.previewData = createPreview(
      { [DungeonDifficulty.Normal]: cleared('2026-08-01T10:00:00Z') },
      [DungeonDifficulty.Normal, DungeonDifficulty.Heroic],
    );

    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Normal);
  });

  it('keeps a manual selection when the preview refreshes', () => {
    const component = createComponent();
    component.previewData = createPreview({
      [DungeonDifficulty.Mythic]: cleared('2026-08-05T10:00:00Z'),
    });
    component.ngOnChanges({});

    component.selectDifficulty(DungeonDifficulty.Normal);
    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Normal);
  });

  it('reapplies the default when a different dungeon is bound', () => {
    const component = createComponent();
    component.previewData = createPreview({
      [DungeonDifficulty.Heroic]: cleared('2026-08-05T10:00:00Z'),
    });
    component.ngOnChanges({});
    component.selectDifficulty(DungeonDifficulty.Normal);

    const other = createPreview({
      [DungeonDifficulty.Mythic]: cleared('2026-08-06T10:00:00Z'),
    });
    other.familyId = 'hives_abyss';
    component.previewData = other;
    component.ngOnChanges({});

    expect(component.difficulty()).toBe(DungeonDifficulty.Mythic);
  });
});
