import { firstValueFrom } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { GatheringType } from '../../../../shared/models/enums/gatheringType';
import { RegionService } from './region.service';

describe('RegionService', () => {
  it('exposes all Lumo Ruins gathering node types', async () => {
    const service = new RegionService({} as ApiService);

    const region = await firstValueFrom(service.getRegionById('shenic'));
    const lumoRuins = region.areas.find(
      (area) => area.id === 'region_01_area_01',
    );

    expect(lumoRuins?.gatheringTypes).toEqual([
      GatheringType.Mining,
      GatheringType.Woodcutting,
      GatheringType.Skinning,
    ]);
  });

  it('exposes Meran and its Tower Floor 10 requirement', async () => {
    const service = new RegionService({} as ApiService);

    const region = await firstValueFrom(service.getRegionById('meran'));

    expect(region.name).toBe('Meran');
    expect(region.requiredTowerFloor).toBe(10);
    expect(region.areas.map((area) => area.name)).toEqual([
      'Warfang Frontier',
      'Rotgrave Fields',
    ]);
    expect(region.areas[0].creatures).toEqual([
      'Gnoll Pack Leader',
      'Gnoll Raider',
      'Gnoll Shaman',
      'Kobold Skirmisher',
      'Kobold Sorcerer',
    ]);
    expect(region.areas[0].gatheringTypes).toEqual([
      GatheringType.Mining,
      GatheringType.Woodcutting,
      GatheringType.Skinning,
    ]);
    expect(region.areas[1].gatheringTypes).toEqual([GatheringType.Mining]);
  });

  it('resolves the parent region from an area id', () => {
    const service = new RegionService({} as ApiService);

    expect(service.getRegionNameByAreaId('region_01_area_01')).toBe('Shenic');
    expect(service.getRegionNameByAreaId('region_02_area_02')).toBe('Meran');
    expect(service.getRegionNameByAreaId('unknown_area')).toBeNull();
  });
});
