import { firstValueFrom, of } from 'rxjs';
import { ApiService } from '../../api/api.service';
import { GatheringType } from '../../../../shared/models/enums/gatheringType';
import { RegionService } from './region.service';

describe('RegionService', () => {
  it('exposes all Lumo Ruins gathering node types', async () => {
    const service = createService();

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
    const service = createService();

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
    expect(region.areas[0].gatheringTypes).toEqual([
      GatheringType.Mining,
      GatheringType.Woodcutting,
      GatheringType.Skinning,
    ]);
    expect(region.areas[1].gatheringTypes).toEqual([GatheringType.Mining]);
    expect(region.areas[2].creatures).toEqual([
      'Blood Harpy',
      'Flame Harpy',
      'Ice Harpy',
      'Shadow Harpy',
      'Wind Harpy',
    ]);
    expect(region.areas[2].gatheringTypes).toEqual([
      GatheringType.Woodcutting,
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
    const service = createService();

    expect(service.getRegionNameByAreaId('region_01_area_01')).toBe('Shenic');
    expect(service.getRegionNameByAreaId('region_02_area_02')).toBe('Meran');
    expect(service.getRegionNameByAreaId('region_02_area_03')).toBe('Meran');
    expect(service.getRegionNameByAreaId('region_02_area_04')).toBe('Meran');
    expect(service.getRegionNameByAreaId('unknown_area')).toBeNull();
  });

  it('merges authoritative gathering nodes into the static region layout', async () => {
    const apiService = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
    apiService.get.and.returnValue(
      of({
        areas: [
          {
            id: 'region_01_area_02',
            gatheringNodes: [
              {
                id: 'blood_grove_ore_vein',
                name: 'Ore Vein',
                type: GatheringType.Mining,
                procChance: 0.0037,
                yieldBonusPercent: 0,
                minQuantity: 8,
                maxQuantity: 24,
              },
            ],
          },
        ],
      }),
    );
    const service = new RegionService(apiService);

    const region = await firstValueFrom(service.getRegionById('shenic'));

    expect(apiService.get).toHaveBeenCalledOnceWith('Region/1/gathering');
    expect(
      region.areas.find((area) => area.id === 'region_01_area_02')
        ?.gatheringNodes,
    ).toEqual([
      jasmine.objectContaining({
        id: 'blood_grove_ore_vein',
        procChance: 0.0037,
        minQuantity: 8,
        maxQuantity: 24,
      }),
    ]);
  });
});

function createService(): RegionService {
  const apiService = jasmine.createSpyObj<ApiService>('ApiService', ['get']);
  apiService.get.and.returnValue(of({ areas: [] }));
  return new RegionService(apiService);
}
