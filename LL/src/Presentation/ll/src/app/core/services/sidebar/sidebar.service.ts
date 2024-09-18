import { Injectable } from '@angular/core';
import { BehaviorSubject, filter, Observable, of } from 'rxjs';
import { SidebarItem } from '../../../shared/models/sidebar-item';
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
    if (currentRoute.includes('team')) return 'team';
    if (currentRoute.includes('town')) return 'town';
    if (currentRoute.includes('dungeons')) return 'dungeons';
    if (currentRoute.includes('quests')) return 'quests';
    if (currentRoute.includes('guild')) return 'guild';
    return 'default'; // Fallback value
  }

  updateContent(content: string) {
    this.sidebarContentSource.next(content);
  }

  // Simulate fetching dynamic items from an API (replace with actual HTTP requests in practice)
  getSidebarItems(): Observable<SidebarItem[]> {
    // Replace with an actual API request using HttpClient
    // This mock returns the items with the correct structure
    const sidebarItems: SidebarItem[] = [
      {
        id: '1',
        route: 'professions/fishing',
        icon: 'path/to/dynamic-quest-icon.png',
        title: 'Fishing',
        description: 'LV 0/100',
        rewards: [
          { icon: 'path/to/fire-icon.png', amount: 5 },
          { icon: 'path/to/coin-icon.png', amount: 3 },
        ],
      },
      {
        id: '2',
        route: 'professions/foraging',
        icon: 'path/to/dynamic-quest-icon.png',
        title: 'Foraging',
        description: '1',
        rewards: [
          { icon: 'path/to/fire-icon.png', amount: 4 },
          { icon: 'path/to/coin-icon.png', amount: 2 },
        ],
      },
      {
        id: '3',
        route: 'professions/mining',
        icon: 'path/to/dynamic-quest-icon.png',
        title: 'Mining',
        description: 'LV 0/100',
        rewards: [
          { icon: 'path/to/fire-icon.png', amount: 5 },
          { icon: 'path/to/coin-icon.png', amount: 3 },
        ],
      },
      {
        id: '4',
        route: 'professions/woodcutting',
        icon: 'path/to/dynamic-quest-icon.png',
        title: 'Woodcutting',
        description: 'LV 0/100',
        rewards: [
          { icon: 'path/to/fire-icon.png', amount: 4 },
          { icon: 'path/to/coin-icon.png', amount: 2 },
        ],
      },
    ];

    // Return the mock data wrapped in an Observable
    return of(sidebarItems);
  }
}
