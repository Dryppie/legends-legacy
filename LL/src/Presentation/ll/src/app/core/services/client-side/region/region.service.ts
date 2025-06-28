import { Injectable } from '@angular/core';
import { ApiService } from '../../api/api.service';
import { Observable, of } from 'rxjs';
import { Region } from '../../../../shared/models/Dtos/regionDto';

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
          id: 'region_01_area_01',
          name: 'Lumo Ruins',
          levelRequirement: 1,
          creatures: ['Goblin', 'Goblin Archer', 'Goblin Warrior', 'Large Rat'],
          description:
            'The Lumo Ruins are crumbling remnants of a forgotten kingdom, overrun by goblins and vermin. Whispers of ancient magic still echo through the cracked stone corridors.',
        },
        {
          id: 'region_01_area_02',
          name: 'Blood Grove',
          levelRequirement: 5,
          creatures: ['Flame Imp', 'Frost Imp', 'Shadow Imp', 'Vampire Bat'],
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
          id: 'region_01_area_05',
          name: 'Goblin Mines',
          levelRequirement: 20,
          creatures: ['Hobgoblin'],
          description:
            'Deep beneath the hills, the Goblin Mines echo with the clang of stolen tools. Hobgoblins rule here, digging for ancient relics they barely understand.',
        },
        {
          id: 'region_01_area_07',
          name: 'Forgotten Ruins',
          levelRequirement: 30,
          creatures: ['Hobgoblin'],
          description:
            'Deep beneath the hills, the Goblin Mines echo with the clang of stolen tools. Hobgoblins rule here, digging for ancient relics they barely understand.',
        },
      ],
      dungeons: [],
      raids: [],
    };

    return shenicRegion;
  }
}
