import { Router } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { DungeonStateService } from '../../../../core/services/api/dungeon/dungeon-state.service';
import {
  DungeonPreviewData,
  DungeonRecord,
} from '../../../models/Dtos/dungeons/dungeonPreviewData';
import { DungeonDifficulty } from '../../../models/enums/dungeonDifficulty';
import { EquipmentType } from '../../../models/enums/equipmentType';
import { ItemType } from '../../../models/enums/itemType';
import { Rarity } from '../../../models/enums/rarity';
import { EssenceItemViewService } from '../../../../core/services/api/essences/essence-item-view.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
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

  for (const [level, chance, section] of [
    [0, 50, 'Chance Drops'],
    [5, 75, 'Chance Drops'],
    [10, 100, 'Guaranteed'],
  ] as const) {
    it(`renders equipment at ${chance}% with mastery ${level} on every difficulty, even without item rewards`, () => {
      TestBed.configureTestingModule({
        imports: [DungeonCardComponent],
        providers: [
          {
            provide: DungeonStateService,
            useValue: {
              sigilAssemblyCost: () => 10,
              sigilFragments: () => 0,
              sigilAssemblyEnabled: () => false,
              activeDungeon: () => null,
            },
          },
          { provide: Router, useValue: {} },
        ],
      });
      const fixture = TestBed.createComponent(DungeonCardComponent);
      const preview = createPreview({});
      for (const variant of Object.values(preview.difficultyVariants!)) {
        variant.equipmentDropChancePercent = chance;
        variant.mastery = { level, experience: 0, completionCount: 0 };
      }
      fixture.componentRef.setInput('previewData', preview);
      fixture.detectChanges();

      for (const difficulty of fixture.componentInstance.difficulties) {
        fixture.componentInstance.selectDifficulty(difficulty);
        fixture.detectChanges();
        const host: HTMLElement = fixture.nativeElement;
        const row = host.querySelector('[data-testid="equipment-reward"]');
        expect(host.querySelectorAll('[data-testid="equipment-reward"]').length).toBe(1);
        expect(row?.textContent).toContain('Equipment');
        expect(row?.textContent).toContain(`${chance}%`);
        expect(row?.textContent).toContain(`Includes Mastery Lv. ${level}`);
        expect(row?.closest('.reward-section')?.querySelector('.reward-section-title')?.textContent).toContain(section);
        expect(host.textContent).toContain('Run Rewards');
        expect(host.textContent).not.toContain('No rewards listed.');
        expect(host.querySelector('.reward-card-header')?.textContent?.replace(/\s+/g, ' ')).toContain('1 item');
      }
    });
  }

  for (const [chance, notice, section] of [
    [25, 'Blueprint chance: 25%. Guaranteed within 4 clears.', 'Chance Drops'],
    [100, 'A blueprint is guaranteed on this clear.', 'Guaranteed'],
  ] as const) {
    it(`shows the named blueprint and its ${chance}% current chance beside equipment`, () => {
      TestBed.configureTestingModule({
        imports: [DungeonCardComponent],
        providers: [
          { provide: DungeonStateService, useValue: {
            sigilAssemblyCost: () => 10, sigilFragments: () => 0,
            sigilAssemblyEnabled: () => false, activeDungeon: () => null,
          } },
          { provide: Router, useValue: {} },
          { provide: EssenceItemViewService, useValue: {} },
          { provide: InventoryStateService, useValue: { isFavorite: () => false, items: () => [] } },
          { provide: EquipmentStateService, useValue: { equipmentSlots: () => [] } },
        ],
      });
      const fixture = TestBed.createComponent(DungeonCardComponent);
      const preview = createPreview({});
      for (const variant of Object.values(preview.difficultyVariants!)) {
        variant.equipmentDropChancePercent = 75;
        variant.mastery = { level: 5, experience: 0, completionCount: 0 };
        variant.rewards = [{
          id: 'execution-reward', category: 'Blueprints', source: notice,
          minQuantity: 1, maxQuantity: 1, dropChancePercent: chance,
          itemBase: {
            id: 'item.blueprint_execution', name: 'Blueprint: Execution',
            rarity: Rarity.Rare, itemType: ItemType.Resource,
            description: 'Consumed to apply Execution to compatible equipment.', stackable: true,
          },
        }];
      }
      fixture.componentRef.setInput('previewData', preview);
      fixture.detectChanges();
      for (const difficulty of fixture.componentInstance.difficulties) {
        fixture.componentInstance.selectDifficulty(difficulty);
        fixture.detectChanges();
        const host: HTMLElement = fixture.nativeElement;
        const blueprint = host.querySelector('app-item')?.closest('.reward-row');
        expect(blueprint?.textContent).toContain('Blueprint: Execution');
        expect(blueprint?.textContent).toContain(`${chance}%`);
        expect(blueprint?.closest('.reward-section')?.textContent).toContain(section);
        expect(host.textContent).toContain(notice);
        expect(host.textContent).not.toContain('Blueprint Choice');
        expect(host.querySelector('[data-testid="equipment-reward"]')?.textContent).toContain('75%');
      }
    });
  }

  it('uses the authoritative equipment chance and omits unavailable drops', () => {
    const component = createComponent();
    component.previewData = createPreview({}, [DungeonDifficulty.Normal]);
    const variant = component.previewData.difficultyVariants![DungeonDifficulty.Normal]!;
    variant.mastery = { level: 5, experience: 0, completionCount: 0 };
    variant.equipmentDropChancePercent = 75;
    expect(component.rewardChanceValueLabel(component.selectedEquipmentReward()!)).toBe('75%');

    for (const chance of [undefined, null, 0, Number.NaN]) {
      variant.equipmentDropChancePercent = chance;
      expect(component.selectedEquipmentReward()).toBeNull();
    }
  });
});
