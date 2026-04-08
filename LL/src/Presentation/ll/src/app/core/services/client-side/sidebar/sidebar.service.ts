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

    if (exact) {
      return currentUrl === targetUrl;
    }

    return currentUrl.startsWith(targetUrl);
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
          description: 'Attributes and essences',
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
          description: 'Absorb and remove essences',
        },
        {
          id: 'soulstone-archive',
          route: ['character', 'soulstone-archive'],
          icon: 'character/essences',
          title: 'Soulstone Archives',
          description: 'Soulstone upgrades',
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
          title: 'World',
          description: 'Travel through the world',
        },
      ],
    },
    {
      id: 'professions',
      label: 'Professions',
      items: [
        {
          id: 'mining',
          route: ['professions', 'gathering', 'mining'],
          icon: 'professions/mining',
          title: 'Mining',
          description: 'Gather ore and rare minerals',
        },
        {
          id: 'woodcutting',
          route: ['professions', 'gathering', 'woodcutting'],
          icon: 'professions/woodcutting',
          title: 'Woodcutting',
          description: 'Harvest wood and natural resources',
        },
        {
          id: 'armorforging',
          route: ['professions', 'crafting', 'armorforging'],
          icon: 'professions/mining',
          title: 'Armorforging',
          description: 'Craft defensive gear',
        },
        {
          id: 'jewelrycrafting',
          route: ['professions', 'crafting', 'jewelrycrafting'],
          icon: 'professions/mining',
          title: 'Jewelrycrafting',
          description: 'Craft rings, amulets, and trinkets',
        },
        {
          id: 'weaponsmithing',
          route: ['professions', 'crafting', 'weaponsmithing'],
          icon: 'professions/mining',
          title: 'Weaponsmithing',
          description: 'Forge offensive equipment',
        },
      ],
    },
    {
      id: 'social',
      label: 'Social',
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
      ],
    },
    {
      id: 'economy',
      label: 'Economy',
      items: [
        {
          id: 'market-place',
          route: ['city', 'market-place'],
          icon: 'city/temple',
          title: 'Cinder Bazaar',
          description: 'List and buy items',
        },
      ],
    },
  ];
}
