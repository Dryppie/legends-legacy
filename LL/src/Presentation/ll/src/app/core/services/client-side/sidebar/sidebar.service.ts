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
          icon: 'character/achievements',
          title: 'Overview',
          description: 'Stats, vitals, loadout',
        },
        {
          id: 'inventory',
          route: ['character', 'inventory'],
          icon: 'character/inventory',
          title: 'Inventory',
          description: 'Items, gear, misc',
        },
        {
          id: 'essences',
          route: ['character', 'essences'],
          icon: 'character/essences',
          title: 'Essences',
          description: 'Archive, attune, ascend',
        },
        {
          id: 'achievements',
          route: ['character', 'achievements'],
          icon: 'character/achievements',
          title: 'Achievements',
          description: 'Records and titles',
        },
        {
          id: 'soulstone-archive',
          route: ['character', 'soulstone-archive'],
          icon: 'character/essences',
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
          icon: 'world/Quest',
          title: 'World Map',
          description: 'Travel and explore',
        },
        {
          id: 'prophecies',
          route: ['prophecies'],
          icon: 'world/Quest',
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
          icon: 'professions/mining',
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
          icon: 'city/temple',
          title: 'Guild',
          description: 'Guild headquarters',
        },
        {
          id: 'colosseum',
          route: ['city', 'colosseum'],
          icon: 'city/temple',
          title: 'Colosseum',
          description: 'Tournaments and battles',
        },
        {
          id: 'market-place',
          route: ['city', 'market-place'],
          icon: 'city/temple',
          title: 'Cinder Bazaar',
          description: 'List and buy items',
        },
        {
          id: 'tavern',
          route: ['city', 'tavern'],
          icon: 'city/temple',
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
          icon: 'city/temple',
          title: 'Settings',
          description: 'Account and preferences',
        },
      ],
    },
  ];
}
