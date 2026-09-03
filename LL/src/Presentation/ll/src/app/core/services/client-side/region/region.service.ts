import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { Region } from '../../../../shared/models/Dtos/regionDto';

@Injectable({
  providedIn: 'root',
})
export class RegionService {
  private readonly firstRegionId = 'shenic';
  private readonly regionIdByAreaId: ReadonlyMap<string, string> = new Map([
    ...this.getShenicRegion().areas.map((area) => [area.id, 'shenic'] as const),
    ...this.getMeranRegion().areas.map((area) => [area.id, 'meran'] as const),
  ]);
  private readonly regionIds: ReadonlySet<string> = new Set(
    this.regionIdByAreaId.values(),
  );
  private readonly regionNameByAreaId: ReadonlyMap<string, string> = new Map(
    [this.getShenicRegion(), this.getMeranRegion()].flatMap((region) =>
      region.areas.map((area) => [area.id, region.name]),
    ),
  );

  public getFirstRegionId(): string {
    return this.firstRegionId;
  }

  public getRegionIdByAreaId(areaId: string): string | null {
    return this.regionIdByAreaId.get(areaId) ?? null;
  }

  public isRegionId(regionId: string): boolean {
    return this.regionIds.has(regionId);
  }

  public getRegionNameByAreaId(areaId: string): string | null {
    return this.regionNameByAreaId.get(areaId) ?? null;
  }

  public getRegionById(id: string): Observable<Region> {
    let region: Region = { name: '', areas: [], dungeons: [], raids: [] };
    if (id.includes('shenic')) {
      region = this.getShenicRegion();
    } else if (id.includes('meran')) {
      region = this.getMeranRegion();
    }
    return of(region);
  }

  private getShenicRegion(): Region {
    let shenicRegion: Region = {
      name: 'Shenic',
      areas: [
        {
          id: 'tutorial_area_training_grounds',
          name: 'Training Area',
          levelRequirement: 1,
          creatures: ['Training Goblin'],
        },
        {
          id: 'region_01_area_01',
          name: 'Lumo Ruins',
          levelRequirement: 1,
          creatures: [
            'Lumo Wisp',
            'Lumo Sentinel',
            'Goblin',
            'Goblin Archer',
            'Goblin Warrior',
          ],
        },
        {
          id: 'region_01_area_02',
          name: 'Blood Grove',
          levelRequirement: 5,
          creatures: [
            'Vampire Bat',
            'Raven',
            'Venomous Snake',
            'Nightshade Blossom',
            'Blood Zombie',
          ],
        },
        {
          id: 'region_01_area_03',
          name: 'Crystal Creek',
          levelRequirement: 10,
          creatures: [
            'Frost Imp',
            'Crystal Wisp',
            'Blue Slime',
            'Transparent Slime',
            'Moss Lizard',
          ],
        },
        {
          id: 'region_01_area_04',
          name: 'Moonlit Graves',
          levelRequirement: 15,
          creatures: [
            'Shadow Imp',
            'Grave Hound',
            'Lost Soul',
            'Grave Wisp',
            'Skeleton',
          ],
        },
        {
          id: 'region_01_area_06',
          name: 'Twilight Clearing',
          levelRequirement: 20,
          creatures: [
            'Pixie',
            'Wood Nymph',
            'Rainbow Slime',
            'Enchanted Fairy',
            'Illusion Fox',
          ],
        },
        {
          id: 'region_01_area_08',
          name: 'Old Forest',
          levelRequirement: 25,
          creatures: [
            'Thornback Boar',
            'Hollow Stag',
            'Treant Sapling',
            'Glade Panther',
            'Forest Spirit',
          ],
        },
        {
          id: 'region_01_area_09',
          name: 'Thornroot Hollow',
          levelRequirement: 30,
          creatures: [
            'Rotroot Shambler',
            'Spider',
            'Giant Spider',
            'Venomous Spiderling',
            'Blackjaw Spider',
          ],
        },
        {
          id: 'region_01_area_10',
          name: 'Embercap Burrows',
          levelRequirement: 35,
          creatures: [
            'Flame Imp',
            'Smolder Rat',
            'Cinder Beetle',
            'Red Slime',
            'Giant Worm',
          ],
        },
        {
          id: 'region_01_area_11',
          name: 'Moonveil Marsh',
          levelRequirement: 40,
          creatures: [
            'Bog Mite',
            'Green Slime',
            'Large Rat',
            'Viper',
            'Poisonous Rat',
          ],
        },
        {
          id: 'region_01_area_07',
          name: 'Duskmire Hollow',
          levelRequirement: 45,
          creatures: [
            'Rotfly Toad',
            'Brown Slime',
            'Cave Bat',
            'Giant Bat',
            'Undead',
          ],
        },
      ],
      dungeons: [],
      raids: [],
    };

    return shenicRegion;
  }

  private getMeranRegion(): Region {
    return {
      name: 'Meran',
      requiredTowerFloor: 10,
      areas: [
        {
          id: 'region_02_area_01',
          name: 'Warfang Frontier',
          levelRequirement: 50,
          creatures: [
            'Gnoll Pack Leader',
            'Gnoll Raider',
            'Gnoll Shaman',
            'Kobold Skirmisher',
            'Kobold Sorcerer',
          ],
        },
        {
          id: 'region_02_area_02',
          name: 'Rotgrave Fields',
          levelRequirement: 55,
          creatures: [
            'Feral Ghoul',
            'Plague Ghoul',
            'Ravenous Ghoul',
            'Vampire Fledgeling',
            'Wandering Ghost',
          ],
        },
        {
          id: 'region_02_area_03',
          name: 'Tempest Aerie',
          levelRequirement: 60,
          creatures: [
            'Blood Harpy',
            'Flame Harpy',
            'Ice Harpy',
            'Shadow Harpy',
            'Wind Harpy',
          ],
        },
        {
          id: 'region_02_area_04',
          name: 'Wolfsbane Reach',
          levelRequirement: 65,
          creatures: [
            'Alpha Wolf',
            'Dire Wolf',
            'Horned Wolf',
            'Bloodfang Wolf',
            'Pack Howler',
          ],
        },
      ],
      dungeons: [],
      raids: [],
    };
  }
}
