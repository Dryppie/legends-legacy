import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { Observable, of } from 'rxjs';
import { Region } from '../../../../shared/models/Dtos/regionDto';
import { GatheringType } from '../../../../shared/models/enums/gatheringType';

@Injectable({
  providedIn: 'root',
})
export class RegionService {
  constructor(private apiService: ApiService) {}

  public getRegionById(id: string): Observable<Region> {
    let region: Region = { name: '', areas: [], dungeons: [], raids: [] };
    if (id.includes('shenic')) {
      region = this.getShenicRegion();
    } else if (id.includes('meran')) {
      region = this.getMeranRegion();
    }
    // else if (id.includes('varnel')) {
    //   region = getCitySidebar();
    // }

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
          gatheringTypes: [
            GatheringType.Mining,
            GatheringType.Woodcutting,
            GatheringType.Skinning,
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
          gatheringTypes: [GatheringType.Woodcutting],
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
          gatheringTypes: [GatheringType.Mining],
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
          gatheringTypes: [GatheringType.Mining, GatheringType.Skinning],
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
          gatheringTypes: [GatheringType.Woodcutting],
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
          gatheringTypes: [GatheringType.Woodcutting, GatheringType.Skinning],
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
          gatheringTypes: [GatheringType.Woodcutting],
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
          gatheringTypes: [GatheringType.Mining],
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
          gatheringTypes: [GatheringType.Skinning],
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
          gatheringTypes: [GatheringType.Mining],
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
      ],
      dungeons: [],
      raids: [],
    };
  }
}
