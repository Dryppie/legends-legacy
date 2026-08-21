import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AchievementService } from '../../../../core/services/api/achievements/achievement.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import {
  AchievementOverviewDto,
  EquippedTitleDto,
  TitleDto,
} from '../../../../shared/models/achievement';
import {
  AchievementsComponent,
  collapseAchievementChains,
} from './achievements.component';
import { AchievementDto } from '../../../../shared/models/achievement';

function achievement(
  key: string,
  requiredAmount: number,
  isCompleted: boolean,
): AchievementDto {
  return {
    key,
    name: key,
    description: 'Defeat monsters',
    category: 'Combat',
    type: 'Milestone',
    scope: 'Account',
    visibility: 'Visible',
    rarity: 'Common',
    requirementType: 'MonstersDefeated',
    requirementTarget: null,
    requiredAmount,
    currentAmount: 1000,
    points: 10,
    isCompleted,
  };
}

describe('collapseAchievementChains', () => {
  it('shows the first incomplete achievement and reports its chain position', () => {
    const result = collapseAchievementChains([
      achievement('monster-hunter-1', 1000, true),
      achievement('monster-hunter-2', 100000, false),
      achievement('monster-hunter-3', 1000000, false),
    ]);

    expect(result).toHaveSize(1);
    expect(result[0]).toEqual(
      jasmine.objectContaining({
        key: 'monster-hunter-2',
        chainPosition: 2,
        chainLength: 3,
      }),
    );
  });

  it('keeps the final achievement visible after completing a chain', () => {
    const result = collapseAchievementChains([
      achievement('monster-hunter-1', 1000, true),
      achievement('monster-hunter-2', 100000, true),
    ]);

    expect(result[0].key).toBe('monster-hunter-2');
    expect(result[0].isCompleted).toBeTrue();
  });

  it('shows the latest unlocked tier when browsing unlocked achievements', () => {
    const result = collapseAchievementChains(
      [
        achievement('monster-hunter-1', 1000, true),
        achievement('monster-hunter-2', 100000, false),
      ],
      'latestCompleted',
    );

    expect(result[0]).toEqual(
      jasmine.objectContaining({
        key: 'monster-hunter-1',
        chainPosition: 1,
        chainLength: 2,
      }),
    );
  });
});

describe('AchievementsComponent title position', () => {
  let fixture: ComponentFixture<AchievementsComponent>;
  let component: AchievementsComponent;
  let achievementsApi: jasmine.SpyObj<AchievementService>;
  let characterState: jasmine.SpyObj<CharacterStateService>;

  const title: TitleDto = {
    key: 'unyielding',
    name: 'Unyielding',
    description: 'Survive a difficult encounter.',
    category: 'Combat',
    rarity: 'Exalted',
    displayPosition: 'Prefix',
    scope: 'Character',
    isUnlocked: true,
    isEquipped: true,
    preview: 'Unyielding admin',
    prefixPreview: 'Unyielding admin',
    suffixPreview: 'admin, the Unyielding',
  };

  const updatedTitle: EquippedTitleDto = {
    key: title.key,
    name: title.name,
    displayPosition: 'Suffix',
    displayName: title.suffixPreview,
  };

  beforeEach(async () => {
    achievementsApi = jasmine.createSpyObj<AchievementService>(
      'AchievementService',
      [
        'getOverview',
        'getAchievements',
        'getTitles',
        'equipTitle',
        'unequipTitle',
      ],
    );
    characterState = jasmine.createSpyObj<CharacterStateService>(
      'CharacterStateService',
      ['updateEquippedTitle', 'refresh'],
    );

    achievementsApi.getOverview.and.returnValue(
      of({} as AchievementOverviewDto),
    );
    achievementsApi.getAchievements.and.returnValue(of([]));
    achievementsApi.getTitles.and.returnValue(of([]));

    await TestBed.configureTestingModule({
      imports: [AchievementsComponent],
      providers: [
        { provide: AchievementService, useValue: achievementsApi },
        { provide: CharacterStateService, useValue: characterState },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AchievementsComponent);
    component = fixture.componentInstance;
  });

  it('persists an equipped title immediately when its position changes', () => {
    achievementsApi.equipTitle.and.returnValue(
      of({
        data: updatedTitle,
        domainVersions: { achievements: 1, character: 1 },
      }),
    );
    component.titles.set([{ ...title }]);

    component.setTitleDisplayPosition('Suffix');

    expect(achievementsApi.equipTitle).toHaveBeenCalledOnceWith(
      title.key,
      'Suffix',
    );
    expect(component.titleDisplayPosition()).toBe('Suffix');
    expect(component.titlePreview(component.equippedTitle()!)).toBe(
      title.suffixPreview,
    );
    expect(component.equipButtonText(component.equippedTitle()!)).toBe(
      'Equipped',
    );
    expect(characterState.updateEquippedTitle).toHaveBeenCalledWith(
      updatedTitle,
    );
  });

  it('restores the previous position when the update fails', () => {
    achievementsApi.equipTitle.and.returnValue(
      throwError(() => new Error('Unable to update title')),
    );
    component.titles.set([{ ...title }]);

    component.setTitleDisplayPosition('Suffix');

    expect(component.titleDisplayPosition()).toBe('Prefix');
    expect(component.error()).toBe('Unable to update title');
  });

  it('initializes the selector from the equipped title', () => {
    achievementsApi.getTitles.and.returnValue(
      of([{ ...title, displayPosition: 'Suffix' }]),
    );

    component.ngOnInit();

    expect(component.titleDisplayPosition()).toBe('Suffix');
  });
});
