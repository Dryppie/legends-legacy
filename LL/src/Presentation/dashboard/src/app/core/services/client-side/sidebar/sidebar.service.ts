import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Tab } from '../../../../shared/models/sidebar-item';
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

  getSidebar(): Tab[] {
    let tabs: Tab[] = [
      {
        label: 'Daily',
        items: [
          {
            id: 'creatures',
            route: 'creatures',
            icon: 'character/achievements',
            title: 'Creatures',
          },
          {
            id: 'items',
            route: 'items',
            icon: 'character/inventory',
            title: 'Items',
          },
          {
            id: 'recipes',
            route: 'recipes',
            icon: 'character/essences',
            title: 'Recipes',
          },
          {
            id: 'diagnostics',
            route: 'diagnostics',
            icon: 'settings/settings',
            title: 'Diagnostics',
            description: 'Combat v2',
          },
        ],
      },
    ];

    return tabs;
  }
}
