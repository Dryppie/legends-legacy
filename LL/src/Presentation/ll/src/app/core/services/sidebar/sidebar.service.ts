import { Injectable } from '@angular/core';
import { BehaviorSubject, filter, Observable, of } from 'rxjs';
import { SidebarItem, Tab } from '../../../shared/models/sidebar-item';
import { Router } from '@angular/router';

@Injectable({
  providedIn: 'root',
})
export class SidebarService {
  private sidebarContentSource = new BehaviorSubject<string>('default');

  currentContent$ = this.sidebarContentSource.asObservable();

  constructor(private router: Router) {
    // Listen for navigation events to set the sidebar content initially
    this.updateContent(this.getContentFromRoute());
  }

  private getContentFromRoute(): string {
    const currentRoute = this.router.url;
    if (currentRoute.includes('character')) return 'character';
    if (currentRoute.includes('professions')) return 'professions';
    if (currentRoute.includes('world')) return 'world';
    if (currentRoute.includes('team')) return 'team';
    if (currentRoute.includes('town')) return 'town';
    if (currentRoute.includes('dungeons')) return 'dungeons';
    if (currentRoute.includes('quests')) return 'quests';
    if (currentRoute.includes('guild')) return 'guild';
    return 'character';
  }

  updateContent(content: string) {
    this.sidebarContentSource.next(content);
  }

  getSidebar(url: string): Observable<Tab[]> {
    let tabs: Tab[] = [];
    if (url.includes('character')) {
      tabs = getCharacterSidebar();
    } else if (url.includes('professions')) {
      tabs = getProfessionSidebar();
    } else if (url.includes('world')) {
      tabs = getWorldSidebar();
    }

    return of(tabs);
  }
}

function getCharacterSidebar(): Tab[] {
  return [
    {
      label: 'Daily',
      items: [
        // {
        //   id: 'inventory',
        //   route: 'character',
        //   icon: 'path/to/quest-icon.png',
        //   title: 'Character Overview',
        //   description: 'Statistics and Equipment',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 2 },
        //     { icon: 'path/to/coin-icon.png', amount: 1 },
        //   ],
        // },
        {
          id: 'inventory',
          route: 'character',
          icon: 'path/to/quest-icon.png',
          title: 'Inventory',
          description: '60/100',
          rewards: [
            { icon: 'path/to/fire-icon.png', amount: 2 },
            { icon: 'path/to/coin-icon.png', amount: 1 },
          ],
        },
        // {
        //   id: '3',
        //   route: 'character/essences',
        //   icon: 'path/to/quest-icon.png',
        //   title: 'Essences',
        //   description: 'More details about the quests',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 2 },
        //     { icon: 'path/to/coin-icon.png', amount: 1 },
        //   ],
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
        // {
        //   id: '3',
        //   route: 'professions/mining',
        //   icon: 'path/to/dynamic-quest-icon.png',
        //   title: 'Mining',
        //   description: 'LV 0/100',
        //   rewards: [
        //     { icon: 'path/to/fire-icon.png', amount: 5 },
        //     { icon: 'path/to/coin-icon.png', amount: 3 },
        //   ],
        // },
        {
          id: 'woodcutting',
          route: 'professions',
          icon: 'path/to/dynamic-quest-icon.png',
          title: 'Woodcutting',
          description: 'LV 0/100',
          rewards: [
            { icon: 'path/to/fire-icon.png', amount: 4 },
            { icon: 'path/to/coin-icon.png', amount: 2 },
          ],
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
          id: '1',
          route: 'world',
          icon: 'path/to/quest-icon.png',
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
