import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgNoticeHarness } from '../../testing/notice.harness';
import { LG_NOTICE, LgNoticeTone } from './notice.component';

@Component({
  imports: [...LG_NOTICE],
  template: `
    <lg-notice
      id="paused"
      [tone]="tone()"
      heading="Offline progress paused"
      [busy]="busy()"
      busyLabel="Catching up"
    >
      The server did not answer.
      <lg-notice-actions
        ><button type="button">Retry</button></lg-notice-actions
      >
    </lg-notice>
    <lg-notice id="bare" heading="Multiplayer access restricted" />
  `,
})
class NoticeHost {
  readonly tone = signal<LgNoticeTone>('danger');
  readonly busy = signal(false);
}

describe('LgNoticeComponent', () => {
  function setup() {
    const fixture = TestBed.createComponent(NoticeHost);
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    return {
      fixture,
      loader: TestbedHarnessEnvironment.loader(fixture),
      notice: el.querySelector<HTMLElement>('#paused')!,
      bare: el.querySelector<HTMLElement>('#bare')!,
    };
  }

  it('is the box itself: a flex row with its tone, and no native tooltip', () => {
    const { notice } = setup();
    expect(notice.classList).toContain('lg-notice');
    expect(notice.classList).toContain('lg-notice--danger');
    expect(getComputedStyle(notice).display).toBe('flex');
    expect(notice.hasAttribute('title')).toBeFalse();
  });

  it('is an alert for danger and a status otherwise', async () => {
    const { fixture, notice, loader } = setup();
    expect(notice.getAttribute('role')).toBe('alert');
    fixture.componentInstance.tone.set('warning');
    fixture.detectChanges();
    expect(notice.getAttribute('role')).toBe('status');
    expect(notice.classList).toContain('lg-notice--warning');
    const harness = await loader.getHarness(
      LgNoticeHarness.with({ heading: 'Offline progress paused' }),
    );
    expect(await harness.getTone()).toBe('warning');
    expect(await harness.getRole()).toBe('status');
  });

  it('says what happened in its heading, the detail under it and the way out at its end', async () => {
    const { notice, loader } = setup();
    const harness = await loader.getHarness(
      LgNoticeHarness.with({ tone: 'danger' }),
    );
    expect(await harness.getHeading()).toBe('Offline progress paused');
    expect(await harness.getText()).toBe('The server did not answer.');
    const actions = notice.lastElementChild as HTMLElement;
    expect(actions.tagName.toLowerCase()).toBe('lg-notice-actions');
    expect(actions.querySelector('button')?.textContent).toBe('Retry');
    expect(getComputedStyle(actions).flexShrink).toBe('0');
  });

  it('without detail is its heading alone', async () => {
    const { bare, loader } = setup();
    const text = bare.querySelector<HTMLElement>('.lg-notice__text')!;
    expect(getComputedStyle(text).display).toBe('none');
    const harness = await loader.getHarness(
      LgNoticeHarness.with({ heading: 'Multiplayer access restricted' }),
    );
    expect(await harness.getText()).toBe('');
    expect(await harness.getTone()).toBe('info');
    expect(await harness.getRole()).toBe('status');
  });

  it('busy adds a named progressbar and marks the notice busy', async () => {
    const { fixture, notice, loader } = setup();
    const harness = await loader.getHarness(
      LgNoticeHarness.with({ heading: 'Offline progress paused' }),
    );
    expect(await harness.isBusy()).toBeFalse();
    expect(notice.hasAttribute('aria-busy')).toBeFalse();
    fixture.componentInstance.busy.set(true);
    fixture.detectChanges();
    expect(await harness.isBusy()).toBeTrue();
    expect(await harness.getBusyLabel()).toBe('Catching up');
    expect(notice.getAttribute('aria-busy')).toBe('true');
    expect(notice.querySelector('.lg-notice__busy')?.getAttribute('role')).toBe(
      'progressbar',
    );
  });
});
