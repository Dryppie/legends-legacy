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
          creatures: ['Goblin', 'Goblin Archer', 'Goblin Warrior', 'Large Rat'],
          gatheringTypes: [
            GatheringType.Mining,
            GatheringType.Woodcutting,
            GatheringType.Skinning,
          ],
          description:
            'The Lumo Ruins are crumbling remnants of a forgotten kingdom, overrun by goblins and vermin. Whispers of ancient magic still echo through the cracked stone corridors.',
        },
        {
          id: 'region_01_area_02',
          name: 'Blood Grove',
          levelRequirement: 5,
          creatures: ['Flame Imp', 'Frost Imp', 'Shadow Imp', 'Vampire Bat'],
          gatheringTypes: [GatheringType.Woodcutting],
          description:
            'The Blood Grove is a cursed forest where the trees bleed sap as red as blood. Twisted imps dance between the roots, feeding off the energy of the living.',
        },
        {
          id: 'region_01_area_03',
          name: 'Crystal Creek',
          levelRequirement: 10,
          creatures: [
            'Blue Slime',
            'Brown Slime',
            'Green Slime',
            'Rainbow Slime',
            'Red Slime',
            'Transparent Slime',
          ],
          description:
            'Crystal Creek shimmers with enchanted waters and glowing minerals. Slimes of every color thrive here, feeding on the creek’s arcane residue.',
        },
        {
          id: 'region_01_area_04',
          name: 'Twilight Clearing',
          levelRequirement: 15,
          creatures: [
            'Enchanted Fairy',
            'Glade Panther',
            'Illusion Fox',
            'Nightshade Blossom',
            'Pixie',
          ],
          description:
            'Bathed in eternal dusk, the Twilight Clearing is a mystical glade where reality bends. It’s a favorite haunt of mischievous fae and creatures born from illusion and light.',
        },
        {
          id: 'region_01_area_06',
          name: 'Oak Thicket',
          levelRequirement: 20,
          creatures: [
            'Moss Lizard',
            'Spider',
            'Treant Sapling',
            'Venomous Snake',
            'Viper',
          ],
          description:
            'Oak Thicket is an old growth woodland where moss-covered predators and venomous creatures hunt beneath roots as thick as fortress walls.',
        },
        {
          id: 'region_01_area_08',
          name: 'Old Forest',
          levelRequirement: 25,
          creatures: [
            'Giant Spider',
            'Venomous Spiderling',
            'Blackjaw Spider',
            'Raven',
            'Widow Stalker',
          ],
          description:
            'The Old Forest is a webbed canopy of ancient trunks, ambush nests, and watchful wings where poison and patience rule the underbrush.',
        },
        {
          id: 'region_01_area_09',
          name: 'Bleak Orchard',
          levelRequirement: 30,
          creatures: ['Scarecrow', 'Lost Soul', 'Apparition', 'Specter'],
          description:
            'Bleak Orchard is a dead stretch of farmland where hollow figures sway in the mist and restless spirits drift between withered trees.',
        },
        {
          id: 'region_01_area_10',
          name: 'Rotting Hamlet',
          levelRequirement: 35,
          creatures: ['Zombie', 'Half Zombie', 'Undead', 'Blood Zombie'],
          description:
            'Rotting Hamlet is a ruined settlement claimed by decay, where the dead linger in broken homes and blood-stained streets.',
        },
        {
          id: 'region_01_area_11',
          name: 'Wormburrow Depths',
          levelRequirement: 40,
          creatures: ['Giant Worm', 'Burrowed Horror', 'Cave Leech', 'Stonejaw Grub', 'Deep Burrower'],
          description:
            'Wormburrow Depths is a collapsed maze of earth and stone where burrowing horrors grind through armor and drain anything that survives the first strike.',
        },
        {
          id: 'region_01_area_07',
          name: 'Forgotten Ruins',
          levelRequirement: 45,
          creatures: [
            'Feral Ghoul',
            'Plague Ghoul',
            'Ravenous Ghoul',
            'Skeleton Archer',
            'Skeleton Mage',
            'Skeleton Warrior',
          ],
          description:
            'Forgotten Ruins are the final broken bones of old Shenic, haunted by ravenous ghouls and skeletal remnants that refuse to rest.',
        },
      ],
      dungeons: [],
      raids: [],
    };

    return shenicRegion;
  }
}
