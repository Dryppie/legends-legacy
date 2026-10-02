import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgRarity } from '../../core/grimoire-core';
import { LG_FOLIO, LgFolioEffect } from './folio.component';

@Component({
  imports: [...LG_FOLIO],
  template: `
    <div style="height: 40rem">
      <lg-folio
        eyebrow="Attribute"
        heading="Wolf"
        headingSub="Ember"
        cornerSrc="corner.svg"
        [label]="label()"
        [rarity]="rarity()"
        [align]="align()"
        [effects]="effects()"
      >
        <lg-folio-emblem><span class="emblem">◆</span></lg-folio-emblem>
        @if (lore()) {
          <lg-folio-lore
            >Found in the ash fields. It hunts in <b>pairs</b>.</lg-folio-lore
          >
        }
        <p class="extra">Stat stack</p>
        <lg-folio-actions
          ><button type="button">Travel</button></lg-folio-actions
        >
        <lg-folio-footer><span class="track">Track</span></lg-folio-footer>
      </lg-folio>
    </div>
  `,
})
class FolioHost {
  readonly label = signal<string | undefined>(undefined);
  readonly rarity = signal<LgRarity | undefined>(undefined);
  readonly align = signal<'center' | 'start'>('center');
  readonly lore = signal(true);
  readonly effects = signal<readonly (string | LgFolioEffect)[] | undefined>([
    'Your attacks burn.',
    { value: '+12%', text: 'damage from equipment' },
  ]);
}

describe('LgFolioComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(FolioHost);
    fixture.detectChanges();
    const folio = (
      fixture.nativeElement as HTMLElement
    ).querySelector<HTMLElement>('lg-folio')!;
    const scroll = folio.querySelector<HTMLElement>('.lg-folio__scroll')!;
    return { fixture, folio, scroll };
  }

  it('is the box itself: a complementary region, named by its heading, Comfortable', () => {
    const { fixture, folio } = setup();
    expect(folio.classList).toContain('lg-folio');
    expect(getComputedStyle(folio).display).toBe('flex');
    expect(folio.getAttribute('role')).toBe('complementary');
    expect(folio.getAttribute('aria-label')).toBe('Wolf');
    expect(folio.getAttribute('data-density')).toBe('comfortable');
    expect(folio.hasAttribute('title')).toBeFalse();

    fixture.componentInstance.label.set('Creature details');
    fixture.detectChanges();
    expect(folio.getAttribute('aria-label')).toBe('Creature details');
  });

  it('sets the emblem, eyebrow and heading at the top, its lighter first words included', () => {
    const { scroll } = setup();
    const top = scroll.firstElementChild as HTMLElement;
    expect(top.classList).toContain('lg-folio__top');
    expect(top.firstElementChild?.tagName.toLowerCase()).toBe(
      'lg-folio-emblem',
    );
    expect(top.querySelector('.lg-folio__eyebrow')?.textContent).toBe(
      'Attribute',
    );
    const h2 = top.querySelector('h2')!;
    expect(h2.classList).toContain('lg-heading--folio');
    expect(h2.textContent?.replace(/\s+/g, ' ').trim()).toBe('Ember Wolf');
  });

  it('orders its regions: top, lore, the ornament rule, effects, other content, actions; the footer under the scroll', () => {
    const { folio, scroll } = setup();
    const order = Array.from(scroll.children).map((c) =>
      c.tagName.toLowerCase(),
    );
    expect(order).toEqual([
      'div',
      'lg-folio-lore',
      'lg-section-rule',
      'ul',
      'p',
      'lg-folio-actions',
    ]);
    const footer = folio.lastElementChild as HTMLElement;
    expect(footer.tagName.toLowerCase()).toBe('lg-folio-footer');
    expect(getComputedStyle(footer).borderTopStyle).toBe('solid');
  });

  it('sets the lore in the lore style, its key nouns in ink', () => {
    const { scroll } = setup();
    const lore = scroll.querySelector<HTMLElement>('lg-folio-lore')!;
    expect(getComputedStyle(lore).fontStyle).toBe('italic');
    expect(getComputedStyle(lore.querySelector('b')!).fontWeight).toBe('500');
  });

  it('draws the ornament rule only with lore or effects', () => {
    const { fixture, scroll } = setup();
    fixture.componentInstance.lore.set(false);
    fixture.detectChanges();
    expect(scroll.querySelector('lg-section-rule')).not.toBeNull();
    fixture.componentInstance.effects.set(undefined);
    fixture.detectChanges();
    expect(scroll.querySelector('lg-section-rule')).toBeNull();
    expect(scroll.querySelector('.lg-folio__effects')).toBeNull();
  });

  it('lists effects as plain lines or a magnitude and its words', () => {
    const { scroll } = setup();
    const items = scroll.querySelectorAll('.lg-folio__effects li');
    expect(items[0].textContent?.trim()).toBe('Your attacks burn.');
    expect(items[1].querySelector('.lg-folio__fx')?.textContent).toBe('+12%');
    expect(items[1].textContent?.trim()).toBe('+12% damage from equipment');
  });

  it('draws the four corner ornaments', () => {
    const { folio } = setup();
    const corners = folio.querySelectorAll<HTMLElement>(':scope > .lg-corner');
    expect(corners.length).toBe(4);
    expect(getComputedStyle(corners[0]).width).toBe('44px');
  });

  it('with a rarity is an item context: a Tag names it and the heading takes its hue', () => {
    const { fixture, folio } = setup();
    fixture.componentInstance.rarity.set('Epic');
    fixture.detectChanges();
    expect(folio.classList).toContain('lg-folio--item');
    expect(folio.classList).toContain('lg-folio--epic');
    expect(
      folio.querySelector('.lg-folio__top lg-tag')?.textContent?.trim(),
    ).toBe('Epic');
    const ink = getComputedStyle(folio).getPropertyValue('--lg-rarity-epic');
    const probe = document.createElement('span');
    probe.style.color = ink;
    document.body.appendChild(probe);
    const epic = getComputedStyle(probe).color;
    probe.remove();
    expect(getComputedStyle(folio.querySelector('h2')!).color).toBe(epic);
  });

  it('align="start" left-aligns the content and its actions', () => {
    const { fixture, folio, scroll } = setup();
    const actions = folio.querySelector<HTMLElement>('lg-folio-actions')!;
    expect(getComputedStyle(scroll).textAlign).toBe('center');
    expect(getComputedStyle(actions).justifyContent).toBe('center');
    fixture.componentInstance.align.set('start');
    fixture.detectChanges();
    expect(folio.classList).toContain('lg-folio--start');
    expect(getComputedStyle(scroll).textAlign).toBe('left');
    expect(getComputedStyle(actions).justifyContent).toBe('flex-start');
  });

  it('its scrolling body is a region of its own', () => {
    const { scroll } = setup();
    expect(getComputedStyle(scroll).containerName).toBe('lg-region');
  });
});
