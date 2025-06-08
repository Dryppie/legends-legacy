import { Injectable } from '@angular/core';
import { BehaviorSubject, filter, Observable, of } from 'rxjs';
import { SidebarItem, Tab } from '../../../../shared/models/sidebar-item';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class SidebarService {
  private sidebarContentSource = new BehaviorSubject<string>('default');

  currentContent$ = this.sidebarContentSource.asObservable();

  constructor(private router: Router) {
    // Listen for navigation events to set the sidebar content initially
    this.updateContent(this.router.url);
  }

  updateContent(content: string) {
    this.sidebarContentSource.next(content);
  }

  getSidebar(url: string): Observable<Tab[]> {
    let tabs: Tab[] = [];
    if (url.includes('character')) {
      tabs = getCharacterSidebar();
    } else if (url.includes('city')) {
      tabs = getCitySidebar();
    } else if (url.includes('professions')) {
      tabs = getProfessionSidebar();
    } else if (url.includes('world')) {
      tabs = getWorldSidebar();
    } else if (url.includes('settings')) {
      tabs = getSettingsSidebar();
    }

    return of(tabs);
  }
}

function getCharacterSidebar(): Tab[] {
  return [
    {
      label: 'Daily',
      items: [
        {
          id: 'character-overview',
          route: ['/game', 'character', 'character-overview'],
          icon: 'character/achievements',
          title: 'Character Overview',
          description: 'Attributes and essences',
        },
        {
          id: 'inventory',
          route: ['/game', 'character', 'inventory'],
          icon: 'character/inventory',
          title: 'Inventory',
          description: 'Items, gear, misc',
        },
        {
          id: 'soulstone-archive',
          route: ['/game', 'character', 'soulstone-archive'],
          icon: 'character/essences',
          title: 'Soulstone Archives',
          description: 'Soulstone upgrades',
        },
        // {
        //   id: 'essences',
        //   route: 'character',
        //   icon: 'character/essences',
        //   title: 'Essences',
        //   description: 'View your essences',
        // },
        // {
        //   id: '4',
        //   route: 'character/achievements',
        //   icon: 'path/to/quest-icon.png',
        //   title: 'Achievements & Titles',
        //   description: 'More details about the quests',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 2 },
        //     { icon: 'path/to/coin-icon.png', amount: 1 },
        //   ],
        // },
      ],
    },
    // {
    //   label: 'Weekly',
    //   items: [
    //     {
    //       id: '1',
    //       route: 'quest/1',
    //       icon: 'path/to/quest-icon.png',
    //       title: 'Wolf Hunting',
    //       description: 'More details about the quests',
    //       rewards: [
    //         { icon: 'path/to/fire-icon.png', amount: 3 },
    //         { icon: 'path/to/coin-icon.png', amount: 2 },
    //       ],
    //     },
    //   ],
    // },
  ];
}

function getCitySidebar(): Tab[] {
  return [
    {
      label: 'City',
      items: [
        {
          id: 'temple',
          route: ['/game', 'city', 'temple'],
          icon: 'city/temple',
          title: 'Temple',
          description: 'Temple',
        },
        {
          id: 'tavern',
          route: ['/game', 'city', 'tavern'],
          icon: 'city/temple',
          title: 'Tavern',
          description: 'Leaderboard',
        },
        {
          id: 'colosseum',
          route: ['/game', 'city', 'colosseum'],
          icon: 'city/temple',
          title: 'Colosseum',
          description: 'Tournaments and Battles',
        },
        {
          id: 'guild',
          route: ['/game', 'city', 'guild'],
          icon: 'city/temple',
          title: 'Guild',
          description: 'Guild headquarters',
        },
        {
          id: 'market-place',
          route: ['/game', 'city', 'market-place'],
          icon: 'city/temple',
          title: 'Cinder Bazaar',
          description: 'List and buy items',
        },
      ],
    },
  ];
}

function getProfessionSidebar(): Tab[] {
  return [
    {
      label: 'Daily',
      items: [
        // {
        //   id: 'fishing',
        //   route: 'professions',
        //   icon: 'path/to/dynamic-quest-icon.png',
        //   title: 'Fishing',
        //   description: 'LV 0/100',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 5 },
        //     { icon: 'path/to/coin-icon.png', amount: 3 },
        //   ],
        // },
        // {
        //   id: '2',
        //   route: 'professions/foraging',
        //   icon: 'path/to/dynamic-quest-icon.png',
        //   title: 'Foraging',
        //   description: '1',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 4 },
        //     { icon: 'path/to/coin-icon.png', amount: 2 },
        //   ],
        // },
        {
          id: 'mining',
          route: ['/game', 'professions', 'gathering', 'mining'],
          icon: 'professions/mining',
          title: 'Mining',
        },
        {
          id: 'woodcutting',
          route: ['/game', 'professions', 'gathering', 'woodcutting'],
          icon: 'professions/woodcutting',
          title: 'Woodcutting',
        },
        {
          id: 'armorforging',
          route: ['/game', 'professions', 'crafting', 'armorforging'],
          icon: 'professions/mining',
          title: 'Armorforging',
        },
        {
          id: 'jewelrycrafting',
          route: ['/game', 'professions', 'crafting', 'jewelrycrafting'],
          icon: 'professions/mining',
          title: 'Jewelrycrafting',
        },
        {
          id: 'weaponsmithing',
          route: ['/game', 'professions', 'crafting', 'weaponsmithing'],
          icon: 'professions/mining',
          title: 'Weaponsmithing',
        },
      ],
    },
  ];
}

function getWorldSidebar(): Tab[] {
  return [
    {
      label: 'World',
      items: [
        {
          id: 'shenic',
          route: ['/game', 'world', 'shenic'],
          icon: 'world/Quest',
          title: 'Shenic',
          description: 'The Shenic Region',
          rewards: [
            { icon: 'path/to/fire-icon.png', amount: 2 },
            { icon: 'path/to/coin-icon.png', amount: 1 },
          ],
        },
        // {
        //   id: '2',
        //   route: 'world',
        //   icon: 'path/to/quest-icon.png',
        //   title: 'Regnia',
        //   description: 'The Regnia Region',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 2 },
        //     { icon: 'path/to/coin-icon.png', amount: 1 },
        //   ],
        // },
      ],
    },
  ];
}

function getSettingsSidebar(): Tab[] {
  return [
    {
      label: 'Settings',
      items: [
        {
          id: 'settings',
          route: ['/game', 'settings'],
          icon: 'world/Quest',
          title: 'Account',
          description: 'Settings',
        },
      ],
    },
  ];
}
