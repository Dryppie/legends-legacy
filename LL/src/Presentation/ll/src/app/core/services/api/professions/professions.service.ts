import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { GatheringNode } from '../../../../shared/models/Dtos/gatheringNode';
import { ApiService } from '../../api/api.service';
import {
  CraftingProfession,
  GatheringProfession,
  Profession,
  Recipe,
} from '../../../../shared/models/profession';
import { ItemType } from '../../../../shared/models/enums/itemType';
import { Rarity } from '../../../../shared/models/enums/rarity';

@Injectable({
  providedIn: 'root',
})
export class ProfessionsService {
  constructor(private apiService: ApiService) {}

  getProfessionById(id: string): Profession {
    if (id.includes('mining')) {
      return this.getMiningProfession();
    }
    if (id.includes('woodcutting')) {
      return this.getWoodcuttingProfession();
    }
    if (id.includes('weaponsmithing')) {
      return this.getWeaponsmithingProfession();
    }
    return { name: '', iconPath: '' };
  }

  getMiningProfession() {
    let miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNodes: this.getMiningNodes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    let woodcuttingProfession: GatheringProfession = {
      name: 'Woodcutting',
      gatheringNodes: this.getWoodcuttingNodes(),
      iconPath: 'woodcutting',
    };
    return woodcuttingProfession;
  }

  getWeaponsmithingProfession() {
    let miningProfession: CraftingProfession = {
      name: 'Weaponsmithing',
      recipes: this.getWeaponsmithingRecipes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWeaponsmithingRecipes(): Recipe[] {
    return [];
  }

  getWoodcuttingNodes(): GatheringNode[] {
    let gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_young_willow',
        name: 'Young Willow',
      },
      {
        id: 'woodcutting_amberleaf_maple',
        name: 'Amberleaf Maple',
      },
      {
        id: 'woodcutting_ember_ash',
        name: 'Ember Ash',
      },
      {
        id: 'woodcutting_moon_birch',
        name: 'Moon Birch',
      },
      {
        id: 'woodcutting_ironwood',
        name: 'Ironwood',
      },
      {
        id: 'woodcutting_sun_cedar',
        name: 'Sun Cedar',
      },
      {
        id: 'woodcutting_frost_pine',
        name: 'Frost Pine',
      },
      // {
      //   id: 'woodcutting_blood_oak',
      //   name: 'Blood Oak',
      // },
      // {
      //   id: 'woodcutting_shadow_willow',
      //   name: 'Shadow Willow',
      // },
      // {
      //   id: 'woodcutting_lightning_elm',
      //   name: 'Lightning Elm',
      // },
      // {
      //   id: 'woodcutting_ancestral_yew',
      //   name: 'Ancestral Yew',
      // },
    ];

    return gatheringNodes;
  }

  getMiningNodes(): GatheringNode[] {
    let gatheringNodes: GatheringNode[] = [
      {
        id: 'woodcutting_tree',
        name: 'Slate Shard',
      },
      {
        id: 'woodcutting_oak',
        name: 'Copperbloom Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Tinspine Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Ironheart Seam',
      },
      {
        id: 'woodcutting_',
        name: 'Silverlight Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Goldflare Vein',
      },
      {
        id: 'woodcutting_',
        name: 'Mithril Thread',
      },
      // {
      //   id: 'woodcutting_',
      //   name: 'Adamant Ridge',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Obsidian Mirror',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Arcanite Cluster',
      // },
      // {
      //   id: 'woodcutting_',
      //   name: 'Dragonstone Core',
      // },
    ];

    return gatheringNodes;
  }
}
