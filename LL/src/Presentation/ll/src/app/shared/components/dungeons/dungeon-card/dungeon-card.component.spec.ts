import { Router } from '@angular/router';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonPreviewData,
  DungeonRecord,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { EquipmentType } from '../../../models/enums/equipmentType';
import { DungeonCardComponent } from './dungeon-card.component';

describe('DungeonCardComponent', () => {
  function createComponent(): DungeonCardComponent {
    return new DungeonCardComponent(
      {
        sigilAssemblyCost: () => 10,
        sigilFragments: () => 0,
      } as unknown as DungeonStateService,
      {} as Router,
    );
  }

  function createComponentWithActiveDungeon(
    dungeonDefinitionId: string,
    navigate = jasmine.createSpy('navigate'),
  ): DungeonCardComponent {
    return new DungeonCardComponent(
      {
        activeDungeon: () => ({ dungeonDefinitionId }),
      } as unknown as DungeonStateService,
      { navigate } as unknown as Router,
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

  it('assembles the selected number of sigils in one request', () => {
    const assembleSigil = jasmine.createSpy('assembleSigil');
    const component = new DungeonCardComponent(
      {
        loading: () => false,
        sigilAssemblyCost: () => 10,
        sigilFragments: () => 85,
        assembleSigil,
      } as unknown as DungeonStateService,
      {} as Router,
    );
    const preview = createPreview({}, [DungeonDifficulty.Normal]);
    preview.canAssembleSigil = true;
    preview.difficultyVariants![DungeonDifficulty.Normal]!.canAssembleSigil =
      true;
    component.previewData = preview;
    component.ngOnChanges({});

    component.setSigilAssemblyQuantity(6);
    component.assembleSelectedSigil();

    expect(component.maximumSigilsAssemblable()).toBe(8);
    expect(assembleSigil).toHaveBeenCalledOnceWith(preview.id, 6);
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

  it('recognizes an active run from any difficulty in the preview family', () => {
    const component = createComponentWithActiveDungeon('goblin_mines_Heroic');
    component.previewData = createPreview({});

    expect(component.isActiveDungeonPreview()).toBeTrue();
  });

  it('continues the active dungeon from its preview', () => {
    const navigate = jasmine.createSpy('navigate');
    const component = createComponentWithActiveDungeon(
      'goblin_mines_Normal',
      navigate,
    );
    component.previewData = createPreview({});

    component.continueDungeon();

    expect(navigate).toHaveBeenCalledOnceWith(['/game/world/dungeon']);
  });

  it('formats dungeon drop chances and quantity ranges explicitly', () => {
    const component = createComponent();
    const reward = {
      minQuantity: 2,
      maxQuantity: 5,
      dropChancePercent: 4.125,
    } as DungeonPreviewData['rewards'][number];

    expect(component.rewardDropChanceLabel(reward)).toBe('4.13% drop');
    expect(component.rewardQuantityLabel(reward)).toBe('Qty 2–5');
  });

  it('combines every tool pool into one random tool reward', () => {
    const component = createComponent();
    const preview = createPreview({}, [DungeonDifficulty.Normal]);
    const toolReward = (
      id: string,
      category: string,
      chance: number,
    ): DungeonPreviewData['rewards'][number] =>
      ({
        id,
        itemBase: {
          id,
          name: id,
          equipmentType: EquipmentType.Tool,
        },
        category,
        source: category,
        minQuantity: 1,
        maxQuantity: 1,
        dropChancePercent: chance,
      }) as unknown as DungeonPreviewData['rewards'][number];
    const rewards = [
      toolReward('pickaxe', 'Completion Loot', 5),
      toolReward('hatchet', 'Completion Loot', 7),
      toolReward('skinning-knife', 'Tier Loot', 10),
    ];
    preview.rewards = rewards;
    preview.difficultyVariants![DungeonDifficulty.Normal]!.rewards = rewards;
    component.previewData = preview;
    component.ngOnChanges({});

    const randomTool = component.selectedRunRewards()[0];

    expect(component.selectedRunRewards().length).toBe(1);
    expect(randomTool.displayName).toBe('Random Tool');
    expect(randomTool.dropChancePercent).toBe(20.8);
    expect(randomTool.minQuantity).toBe(1);
    expect(randomTool.maxQuantity).toBe(1);
  });

  it('labels gathering percentages as base chances', () => {
    const component = createComponent();
    const loot = {
      minQuantity: 1,
      maxQuantity: 1,
      dropChancePercent: 0.37,
    } as NonNullable<
      DungeonPreviewData['gatheringNodes']
    >[number]['loot'][number];

    expect(component.gatheringLootDropChanceLabel(loot)).toBe(
      'Base 0.37% drop',
    );
  });
});
