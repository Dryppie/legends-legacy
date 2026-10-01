import { EMPTY } from 'rxjs';
import { Router } from '@angular/router';
import { SidebarService } from '../../core/services/client-side/sidebar/sidebar.service';
import { SidebarSection } from '../../shared/models/sidebar-item';
import { RAIL_ICONS, railParentId, toRailSections } from './rail-grimoire.component';

describe('toRailSections', () => {
  let sections: SidebarSection[] = [];
  beforeEach(() => {
    new SidebarService({ url: '/game', events: EMPTY } as unknown as Router).getSidebar().subscribe((s) => (sections = s));
  });

  it('has a Grimoire icon for every destination', () => {
    const ids = sections.flatMap((s) => s.items.map((i) => i.id));
    expect(ids.length).toBeGreaterThan(0);
    expect(ids.filter((id) => !RAIL_ICONS[id])).toEqual([]);
  });

  it('keeps titles, descriptions and routes', () => {
    const rail = toRailSections(sections, () => ({ lockReason: null, count: 0, next: false }));
    const first = rail[0].items[0];
    expect(rail[0].label).toBe(sections[0].label);
    expect(first.title).toBe(sections[0].items[0].title);
    expect(first.description).toBe(sections[0].items[0].description);
    expect(first.route).toBe('/game/' + sections[0].items[0].route.join('/'));
    expect(first.locked).toBeUndefined();
  });

  it('shows journey-locked destinations as Locked with the reason', () => {
    const rail = toRailSections(sections, () => ({ lockReason: 'Unlocks at level 10', count: 0, next: false }));
    expect(rail[0].items[0]).toEqual(jasmine.objectContaining({ locked: true, reason: 'Unlocks at level 10' }));
  });

  it('turns counts into badges in words, and the next step into ready', () => {
    const rail = toRailSections(sections, (item) =>
      item.id === 'quests' ? { lockReason: null, count: 2, next: false } : item.id === 'world' ? { lockReason: null, count: 0, next: true } : { lockReason: null, count: 0, next: false },
    );
    const items = rail.flatMap((s) => s.items);
    expect(items.find((i) => i.id === 'quests')).toEqual(jasmine.objectContaining({ badge: 2, badgeLabel: '2 quests ready to turn in' }));
    expect(items.find((i) => i.id === 'world')).toEqual(jasmine.objectContaining({ ready: true, badgeLabel: 'Next on your journey' }));
  });
});

describe('railParentId', () => {
  it('marks the World Map on screens inside the world', () => {
    expect(railParentId('/game/world/dungeon')).toBe('world');
    expect(railParentId('/game/world/raid/r1?tab=log')).toBe('world');
    expect(railParentId('/game/world')).toBe('world');
  });

  it('leaves other screens alone', () => {
    expect(railParentId('/game/character/inventory')).toBeUndefined();
    expect(railParentId('/game/worldly')).toBeUndefined();
  });
});
