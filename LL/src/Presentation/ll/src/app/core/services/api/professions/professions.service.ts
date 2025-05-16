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
import { RECIPES_CONTENT } from '../../../../data/recipes-content';

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
    const miningProfession: GatheringProfession = {
      name: 'Mining',
      gatheringNodes: this.getMiningNodes(),
      iconPath: 'mining',
    };
    return miningProfession;
  }

  getWoodcuttingProfession() {
    const woodcuttingProfession: GatheringProfession = {
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
    return RECIPES_CONTENT;
  }

  getWoodcuttingNodes(): GatheringNode[] {
    const gatheringNodes: GatheringNode[] = [
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
    const gatheringNodes: GatheringNode[] = [
      {
        id: 'mining_slate_shard',
        name: 'Slate Shard',
      },
      {
        id: 'mining_copperbloom_vein',
        name: 'Copperbloom Vein',
      },
      {
        id: 'mining_tinspine_vein',
        name: 'Tinspine Vein',
      },
      {
        id: 'mining_ironheart_seam',
        name: 'Ironheart Seam',
      },
      {
        id: 'mining_silverlight_vein',
        name: 'Silverlight Vein',
      },
      {
        id: 'mining_goldflare_vein',
        name: 'Goldflare Vein',
      },
      {
        id: 'mining_mithril_thread',
        name: 'Mithril Thread',
      },
      // {
      //   id: 'mining_adamant_ridge',
      //   name: 'Adamant Ridge',
      // },
      // {
      //   id: 'mining_obsidian_mirror',
      //   name: 'Obsidian Mirror',
      // },
      // {
      //   id: 'mining_arcanite_cluster',
      //   name: 'Arcanite Cluster',
      // },
      // {
      //   id: 'mining_dragonstone_core',
      //   name: 'Dragonstone Core',
      // },
    ];

    return gatheringNodes;
  }
}
