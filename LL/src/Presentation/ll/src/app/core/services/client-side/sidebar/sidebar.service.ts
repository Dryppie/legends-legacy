import { Injectable } from '@angular/core';
import { BehaviorSubject, filter, Observable, of } from 'rxjs';
import { NavigationEnd, Router } from '@angular/router';
import { SidebarSection } from '../../../../shared/models/sidebar-item';

@Injectable({
  providedIn: 'root',
})
export class SidebarService {
  private readonly activeUrlSource = new BehaviorSubject<string>('');
  readonly activeUrl$ = this.activeUrlSource.asObservable();

  constructor(private readonly router: Router) {
    this.activeUrlSource.next(this.router.url);

    this.router.events
      .pipe(
        filter(
          (event): event is NavigationEnd => event instanceof NavigationEnd,
        ),
      )
      .subscribe((event) => {
        this.activeUrlSource.next(event.urlAfterRedirects);
      });
  }

  getSidebar(): Observable<SidebarSection[]> {
    return of(getSidebarSections());
  }

  getActiveUrl(): string {
    return this.activeUrlSource.value;
  }

  isRouteActive(route: string[], exact = false): boolean {
    const currentUrl = this.getActiveUrl();
    const targetUrl = '/' + route.join('/');
    const gameTargetUrl = '/game' + targetUrl;

    if (exact) {
      return currentUrl === targetUrl || currentUrl === gameTargetUrl;
    }

    return (
      currentUrl.startsWith(targetUrl) || currentUrl.startsWith(gameTargetUrl)
    );
  }
}

function getSidebarSections(): SidebarSection[] {
  return [
    {
      id: 'character',
      label: 'Character',
      items: [
        {
          id: 'character-overview',
          route: ['character', 'character-overview'],
          icon: 'sidebar/character/overview',
          title: 'Overview',
          description: 'Stats, vitals, loadout',
        },
        {
          id: 'inventory',
          route: ['character', 'inventory'],
          icon: 'sidebar/character/inventory',
          title: 'Inventory',
          description: 'Items, gear, misc',
        },
        {
          id: 'essences',
          route: ['character', 'essences'],
          icon: 'sidebar/character/essences',
          title: 'Essences',
          description: 'Archive, attune, ascend',
        },
        {
          id: 'achievements',
          route: ['character', 'achievements'],
          icon: 'sidebar/character/achievements',
          title: 'Achievements',
          description: 'Records and titles',
        },
        {
          id: 'soulstone-archive',
          route: ['character', 'soulstone-archive'],
          icon: 'sidebar/character/soulstones',
          title: 'Soulstones',
          description: 'Permanent upgrades',
        },
      ],
    },
    {
      id: 'world',
      label: 'World',
      items: [
        {
          id: 'world',
          route: ['world', 'shenic'],
          icon: 'sidebar/world/world-map',
          title: 'World Map',
          description: 'Travel and explore',
        },
        {
          id: 'prophecies',
          route: ['prophecies'],
          icon: 'sidebar/world/prophecies',
          title: 'Prophecies',
          description: 'Daily and weekly omens',
        },
      ],
    },
    {
      id: 'professions',
      label: 'Professions',
      items: [
        {
          id: 'crafting',
          route: ['professions', 'crafting'],
          icon: 'sidebar/professions/crafting',
          title: 'Crafting',
          description: 'Craft and temper equipment',
        },
      ],
    },
    {
      id: 'city',
      label: 'City',
      items: [
        {
          id: 'guild',
          route: ['city', 'guild'],
          icon: 'sidebar/city/guild',
          title: 'Guild',
          description: 'Guild headquarters',
          minimumLevel: 10,
        },
        {
          id: 'colosseum',
          route: ['city', 'colosseum'],
          icon: 'sidebar/city/colosseum',
          title: 'Colosseum',
          description: 'Tournaments and battles',
          minimumLevel: 5,
        },
        {
          id: 'market-place',
          route: ['city', 'market-place'],
          icon: 'sidebar/city/cinder-bazaar',
          title: 'Cinder Bazaar',
          description: 'List and buy items',
          minimumLevel: 10,
        },
        {
          id: 'tavern',
          route: ['city', 'tavern'],
          icon: 'sidebar/city/leaderboard',
          title: 'Leaderboard',
          description: 'Rankings and records',
        },
      ],
    },
    // {
    //   id: 'economy',
    //   label: 'Economy',
    //   items: [],
    // },
    {
      id: 'system',
      label: 'System',
      items: [
        {
          id: 'settings',
          route: ['settings'],
          icon: 'sidebar/system/settings',
          title: 'Settings',
          description: 'Account and preferences',
        },
      ],
    },
  ];
}
