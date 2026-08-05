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
          description:
            'A controlled training route for new adventurers. The creature here is weak enough to practice combat and earn your first Essence.',
        },
        {
          id: 'region_01_area_01',
          name: 'Lumo Ruins',
          levelRequirement: 1,
          creatures: ['Lumo Wisp', 'Lumo Sentinel', 'Goblin', 'Goblin Archer', 'Goblin Warrior'],
          gatheringTypes: [
            GatheringType.Mining,
            GatheringType.Woodcutting,
            GatheringType.Skinning,
          ],
          description:
            'The Lumo Ruins are crumbling remnants of a forgotten kingdom, where luminous spirits and sentinels linger among goblin-held halls.',
        },
        {
          id: 'region_01_area_02',
          name: 'Blood Grove',
          levelRequirement: 5,
          creatures: ['Vampire Bat', 'Raven', 'Venomous Snake', 'Nightshade Blossom', 'Blood Zombie'],
          gatheringTypes: [GatheringType.Woodcutting],
          description:
            'The Blood Grove is a cursed forest where the trees bleed crimson sap and its predatory wildlife stalks the restless dead.',
        },
        {
          id: 'region_01_area_03',
          name: 'Crystal Creek',
          levelRequirement: 10,
          creatures: ['Frost Imp', 'Crystal Wisp', 'Blue Slime', 'Transparent Slime', 'Moss Lizard'],
          description:
            'Crystal Creek shimmers with enchanted waters and glowing minerals, drawing frost-touched creatures that feed on its arcane residue.',
        },
        {
          id: 'region_01_area_04',
          name: 'Moonlit Graves',
          levelRequirement: 15,
          creatures: ['Shadow Imp', 'Grave Hound', 'Lost Soul', 'Grave Wisp', 'Skeleton'],
          description:
            'Moonlit Graves is a pale cemetery where grave-born creatures and lost spirits gather beneath an unending moon.',
        },
        {
          id: 'region_01_area_06',
          name: 'Twilight Clearing',
          levelRequirement: 20,
          creatures: ['Pixie', 'Wood Nymph', 'Rainbow Slime', 'Enchanted Fairy', 'Illusion Fox'],
          description:
            'Bathed in eternal dusk, the Twilight Clearing is a mystical glade where mischievous fae and creatures of illusion gather.',
        },
        {
          id: 'region_01_area_08',
          name: 'Old Forest',
          levelRequirement: 25,
          creatures: ['Thornback Boar', 'Hollow Stag', 'Treant Sapling', 'Glade Panther', 'Forest Spirit'],
          description:
            'The Old Forest is an ancient woodland where territorial beasts and spirits guard roots older than Shenic itself.',
        },
        {
          id: 'region_01_area_09',
          name: 'Thornroot Hollow',
          levelRequirement: 30,
          creatures: ['Rotroot Shambler', 'Spider', 'Giant Spider', 'Venomous Spiderling', 'Blackjaw Spider'],
          description:
            'Thornroot Hollow is a tangled sink beneath the forest canopy, overrun by spiders and animated rot.',
        },
        {
          id: 'region_01_area_10',
          name: 'Embercap Burrows',
          levelRequirement: 35,
          creatures: ['Flame Imp', 'Smolder Rat', 'Cinder Beetle', 'Red Slime', 'Giant Worm'],
          description:
            'Embercap Burrows is a scorched tunnel network where heat-loving vermin nest among glowing fungus and molten seams.',
        },
        {
          id: 'region_01_area_11',
          name: 'Moonveil Marsh',
          levelRequirement: 40,
          creatures: ['Bog Mite', 'Green Slime', 'Large Rat', 'Viper', 'Poisonous Rat'],
          description:
            'Moonveil Marsh is a venomous wetland where swollen vermin and marsh predators move beneath luminous fog.',
        },
        {
          id: 'region_01_area_07',
          name: 'Duskmire Hollow',
          levelRequirement: 45,
          creatures: ['Rotfly Toad', 'Brown Slime', 'Cave Bat', 'Giant Bat', 'Undead'],
          description:
            'Duskmire Hollow is a lightless mire of stagnant caves, where bats, slimes, and the undead thrive in the gloom.',
        },
      ],
      dungeons: [],
      raids: [],
    };

    return shenicRegion;
  }
}
