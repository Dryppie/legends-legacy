import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgActivityComponent } from './activity.component';
import { LgActivityHarness } from '../../testing/activity.harness';

/** The parity case i-activity. */
@Component({
  imports: [LgActivityComponent],
  template: `
    <lg-activity
      label="Engaged in Combat"
      remaining="00:12"
      [progress]="0.25"
      interactive
      (activate)="activated = activated + 1"
    />
  `,
})
class ActivityCase {
  activated = 0;
}

describe('LgActivityComponent', () => {
  let fixture: ComponentFixture<ActivityCase>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ActivityCase);
  });

  it('emits activate once on a press when interactive (i-activity)', async () => {
    const activity =
      await TestbedHarnessEnvironment.loader(fixture).getHarness(
        LgActivityHarness,
      );

    expect(await activity.getLabel()).toBe('Engaged in Combat');
    expect(await activity.getRemaining()).toBe('00:12');
    await activity.press();

    expect(fixture.componentInstance.activated).toBe(1);
  });
});
