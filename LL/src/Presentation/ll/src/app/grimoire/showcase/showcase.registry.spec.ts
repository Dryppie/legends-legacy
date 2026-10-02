import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LG_GRIMOIRE } from '@grimoire';
import { SHOWCASE_ENTRIES } from './showcase.registry';
import { showcaseSlug } from './showcase.types';

describe('Grimoire showcase registry', () => {
  beforeEach(() =>
    TestBed.configureTestingModule({ providers: [provideRouter([])] }),
  );

  it('gives every entry a kebab-case slug, unique within its tier', () => {
    const seen = new Set<string>();
    for (const entry of SHOWCASE_ENTRIES) {
      expect(entry.slug).withContext(entry.name).toBe(showcaseSlug(entry.slug));
      const key = `${entry.tier}/${entry.slug}`;
      expect(seen.has(key)).withContext(`${key} appears twice`).toBeFalse();
      seen.add(key);
    }
  });

  it('gives every entry at least one story, each with its own slug', () => {
    for (const entry of SHOWCASE_ENTRIES) {
      const fixture = TestBed.createComponent(entry.component);
      fixture.detectChanges();
      const slugs = fixture.componentInstance.stories().map((s) => s.slug());
      expect(slugs.length)
        .withContext(`${entry.name} has no stories`)
        .toBeGreaterThan(0);
      expect(new Set(slugs).size)
        .withContext(`${entry.name} repeats a story slug`)
        .toBe(slugs.length);
      fixture.destroy();
    }
  });

  it('shows every Grimoire component somewhere', () => {
    const covered = new Set(SHOWCASE_ENTRIES.flatMap((e) => e.covers));
    const components = LG_GRIMOIRE.map((c) => c.name).filter((name) =>
      name.endsWith('Component'),
    );
    expect(components.length)
      .withContext('class names are readable in the test build')
      .toBeGreaterThan(40);
    const missing = components.filter((name) => !covered.has(name));
    expect(missing)
      .withContext('components without a showcase entry')
      .toEqual([]);
  });
});
