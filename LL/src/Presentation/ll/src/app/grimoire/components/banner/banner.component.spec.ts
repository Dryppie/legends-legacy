import { ApplicationRef, Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LG_FOLIO } from '../folio/folio.component';
import { LG_BANNER } from './banner.component';

@Component({
  imports: [...LG_BANNER],
  template: `
    <lg-banner [label]="label()" [image]="image()" [cornerSrc]="corner()">
      <p class="identity">Aldric Vane</p>
      <lg-banner-aside><span class="figure">Level 17</span></lg-banner-aside>
      <lg-banner-footer><p class="perks">Perks</p></lg-banner-footer>
    </lg-banner>
  `,
})
class BannerHost {
  readonly label = signal<string | undefined>('Combat profile');
  readonly image = signal<string | undefined>(undefined);
  readonly corner = signal<string | undefined>(undefined);
}

@Component({
  imports: [...LG_BANNER, ...LG_FOLIO],
  template: `
    <div class="lg-shell">
      <lg-folio heading="Shenic" />
      <lg-banner label="Combat profile" />
    </div>
  `,
})
class FramedTwiceHost {}

describe('LgBannerComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(BannerHost);
    fixture.detectChanges();
    const banner = (
      fixture.nativeElement as HTMLElement
    ).querySelector<HTMLElement>('lg-banner')!;
    return { fixture, banner };
  }

  it('is the box itself: a region named by its label', () => {
    const { fixture, banner } = setup();
    expect(banner.classList).toContain('lg-banner');
    expect(getComputedStyle(banner).display).toBe('block');
    expect(getComputedStyle(banner).overflow).toBe('hidden');
    expect(banner.getAttribute('role')).toBe('region');
    expect(banner.getAttribute('aria-label')).toBe('Combat profile');

    fixture.componentInstance.label.set(undefined);
    fixture.detectChanges();
    expect(banner.hasAttribute('role')).toBeFalse();
    expect(banner.hasAttribute('aria-label')).toBeFalse();
  });

  it('sets the identity in the body, the figures beside it and the footer across the foot', () => {
    const { banner } = setup();
    const body = banner.querySelector<HTMLElement>('.lg-banner__body')!;
    const main = body.querySelector('.lg-banner__main')!;
    expect(main.querySelector('.identity')).not.toBeNull();
    const aside = main.nextElementSibling as HTMLElement;
    expect(aside.tagName.toLowerCase()).toBe('lg-banner-aside');
    expect(aside.parentElement).toBe(body);
    expect(getComputedStyle(aside).display).toBe('flex');
    const footer = banner.lastElementChild as HTMLElement;
    expect(footer.tagName.toLowerCase()).toBe('lg-banner-footer');
    expect(footer.querySelector('.perks')).not.toBeNull();
    expect(getComputedStyle(footer).display).toBe('block');
  });

  it('draws the art and its veil only with an image, and the corners only with cornerSrc', () => {
    const { fixture, banner } = setup();
    expect(banner.querySelector('.lg-banner__art')).toBeNull();
    expect(banner.querySelector('.lg-banner__veil')).toBeNull();
    expect(banner.querySelectorAll('.lg-corner').length).toBe(0);
    expect(banner.querySelector('.lg-banner__frame')).not.toBeNull();

    fixture.componentInstance.image.set('art.webp');
    fixture.componentInstance.corner.set('corner.svg');
    fixture.detectChanges();
    const art = banner.querySelector<HTMLElement>('.lg-banner__art')!;
    expect(art.style.backgroundImage).toContain('art.webp');
    expect(banner.querySelector('.lg-banner__veil')).not.toBeNull();
    const corners = banner.querySelectorAll<HTMLElement>('.lg-corner');
    expect(corners.length).toBe(4);
    expect(getComputedStyle(corners[0]).width).toBe('36px');
    expect(getComputedStyle(corners[0]).position).toBe('absolute');
  });

  it('warns when it shares a GameShell with a Folio: one ornamented framed surface per screen', () => {
    const warn = spyOn(console, 'warn');
    const fixture = TestBed.createComponent(FramedTwiceHost);
    fixture.detectChanges();
    TestBed.inject(ApplicationRef).tick();
    expect(warn).toHaveBeenCalledWith(
      jasmine.stringMatching(/a Folio and a Banner on one screen/),
    );
  });
});
