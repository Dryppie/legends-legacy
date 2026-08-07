import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AchievementService } from '../../../../core/services/api/achievements/achievement.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import {
  AchievementOverviewDto,
  EquippedTitleDto,
  TitleDto,
} from '../../../../shared/models/achievement';
import { AchievementsComponent } from './achievements.component';

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
    achievementsApi.equipTitle.and.returnValue(of(updatedTitle));
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
