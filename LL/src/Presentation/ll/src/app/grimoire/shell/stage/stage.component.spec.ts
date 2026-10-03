import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { LgStageComponent } from './stage.component';

@Component({
  imports: [LgStageComponent],
  template: `<lg-stage [image]="image()" [label]="label()"
    ><p class="sc-scene">Scene</p></lg-stage
  >`,
})
class StageHost {
  readonly image = signal<string | undefined>(
    'assets/backgrounds/optimized/temple.webp',
  );
  readonly label = signal<string | undefined>('World Map');
}

describe('LgStageComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(StageHost);
    fixture.detectChanges();
    return {
      fixture,
      stage: fixture.nativeElement.querySelector('lg-stage') as HTMLElement,
    };
  }

  it('is a region named by its label, the art and veil behind its content', () => {
    const { stage } = setup();

    expect(stage.getAttribute('role')).toBe('region');
    expect(stage.getAttribute('aria-label')).toBe('World Map');
    expect(
      stage.querySelector('.lg-stage__art')?.getAttribute('aria-hidden'),
    ).toBe('true');
    expect(stage.querySelector('.lg-stage__veil')).not.toBeNull();
    expect(stage.querySelector('.lg-stage__content .sc-scene')).not.toBeNull();
  });

  it('without a label it is no landmark, and without art it has no veil', () => {
    const { fixture, stage } = setup();
    fixture.componentInstance.label.set(undefined);
    fixture.componentInstance.image.set(undefined);
    fixture.detectChanges();

    expect(stage.hasAttribute('role')).toBeFalse();
    expect(stage.hasAttribute('aria-label')).toBeFalse();
    expect(stage.querySelector('.lg-stage__art, .lg-stage__veil')).toBeNull();
  });
});
