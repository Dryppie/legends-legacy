import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormsModule } from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgInputComponent } from './input.component';
import { LgInputHarness } from '../../testing/input.harness';

@Component({
  imports: [FormsModule, LgInputComponent],
  template: `
    <label for="own">Search</label>
    <input lgInput id="own" placeholder="Name" [(ngModel)]="name" />
    <input lgInput class="sc-off" value="Locked in" disabled />
  `,
})
class InputHost {
  name = 'Maren';
}

describe('LgInputComponent', () => {
  it('is the native input, its own id kept, and ngModel binds it', async () => {
    const fixture = TestBed.createComponent(InputHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const [input, off] = await loader.getAllHarnesses(LgInputHarness);

    expect(await input.getValue()).toBe('Maren');
    expect(await input.getLabel()).toBe('Search');
    await input.setValue('Kaelen');
    expect(fixture.componentInstance.name).toBe('Kaelen');
    expect(await input.getDescription()).toBeNull();

    expect(await off.isDisabled()).toBeTrue();
    const el: HTMLInputElement = fixture.nativeElement.querySelector('.sc-off');
    expect(el.classList).toContain('lg-input');
    expect(el.hasAttribute('id')).toBeFalse();
  });
});
