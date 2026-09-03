import { firstValueFrom } from 'rxjs';
import { RegionService } from './region.service';

describe('RegionService', () => {
  it('exposes the first expedition with its level requirement and combat roster', async () => {
    const service = new RegionService();

    const region = await firstValueFrom(service.getRegionById('shenic'));
    const lumoRuins = region.areas.find(
      (area) => area.id === 'region_01_area_01',
    );

    expect(lumoRuins?.levelRequirement).toBe(1);
    expect(lumoRuins?.creatures).toEqual([
      'Lumo Wisp',
      'Lumo Sentinel',
      'Goblin',
      'Goblin Archer',
      'Goblin Warrior',
    ]);
  });

  it('exposes Meran and its Tower Floor 10 requirement', async () => {
    const service = new RegionService();

    const region = await firstValueFrom(service.getRegionById('meran'));

    expect(region.name).toBe('Meran');
    expect(region.requiredTowerFloor).toBe(10);
    expect(region.areas.map((area) => area.name)).toEqual([
      'Warfang Frontier',
      'Rotgrave Fields',
      'Tempest Aerie',
      'Wolfsbane Reach',
    ]);
    expect(region.areas[0].creatures).toEqual([
      'Gnoll Pack Leader',
      'Gnoll Raider',
      'Gnoll Shaman',
      'Kobold Skirmisher',
      'Kobold Sorcerer',
    ]);
    expect(region.areas.map((area) => area.levelRequirement)).toEqual([
      50, 55, 60, 65,
    ]);
    expect(region.areas[2].creatures).toEqual([
      'Blood Harpy',
      'Flame Harpy',
      'Ice Harpy',
      'Shadow Harpy',
      'Wind Harpy',
    ]);
    expect(region.areas[3].creatures).toEqual([
      'Alpha Wolf',
      'Dire Wolf',
      'Horned Wolf',
      'Bloodfang Wolf',
      'Pack Howler',
    ]);
  });

  it('resolves the parent region from an area id', () => {
    const service = new RegionService();

    expect(service.getFirstRegionId()).toBe('shenic');
    expect(service.getRegionIdByAreaId('tutorial_area_training_grounds')).toBe(
      'shenic',
    );
    expect(service.getRegionIdByAreaId('region_01_area_01')).toBe('shenic');
    expect(service.getRegionIdByAreaId('region_02_area_02')).toBe('meran');
    expect(service.getRegionIdByAreaId('unknown_area')).toBeNull();
    expect(service.isRegionId('shenic')).toBeTrue();
    expect(service.isRegionId('meran')).toBeTrue();
    expect(service.isRegionId('tower')).toBeFalse();
    expect(service.getRegionNameByAreaId('region_01_area_01')).toBe('Shenic');
    expect(service.getRegionNameByAreaId('region_02_area_02')).toBe('Meran');
    expect(service.getRegionNameByAreaId('region_02_area_03')).toBe('Meran');
    expect(service.getRegionNameByAreaId('region_02_area_04')).toBe('Meran');
    expect(service.getRegionNameByAreaId('unknown_area')).toBeNull();
  });
});
