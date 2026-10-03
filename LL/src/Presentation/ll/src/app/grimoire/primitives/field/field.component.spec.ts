import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { LgFieldComponent, lgFieldMessage } from './field.component';
import { LgInputComponent } from '../input/input.component';
import { LgFieldHarness } from '../../testing/field.harness';

@Component({
  imports: [ReactiveFormsModule, LgFieldComponent, LgInputComponent],
  template: `
    <form [formGroup]="form">
      <lg-field
        label="Character name"
        hint="3 to 16 letters"
        [messages]="{ required: 'Name your character.' }"
      >
        <input lgInput formControlName="name" />
      </lg-field>
      <lg-field label="Note" [error]="serverError()">
        <textarea lgInput formControlName="note"></textarea>
      </lg-field>
    </form>
  `,
})
class FieldHost {
  readonly form = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(3)]),
    note: new FormControl(''),
  });
  readonly serverError = signal<string | null>(null);
}

describe('LgFieldComponent', () => {
  async function setup() {
    const fixture = TestBed.createComponent(FieldHost);
    const loader = TestbedHarnessEnvironment.loader(fixture);
    const name = await loader.getHarness(
      LgFieldHarness.with({ label: 'Character name' }),
    );
    const note = await loader.getHarness(
      LgFieldHarness.with({ label: 'Note' }),
    );
    return { fixture, name, note };
  }

  it('labels its input and describes it with the hint; a required control marks the label', async () => {
    const { name } = await setup();
    const input = await name.getInput();

    expect(await input.getLabel()).toBe('Character name');
    expect(await input.getDescription()).toBe('3 to 16 letters');
    expect(await name.isRequired()).toBeTrue();
    expect(await input.isRequired()).toBeTrue();
    expect(await name.getError()).toBeNull();
    expect(await input.isInvalid()).toBeFalse();
  });

  it('shows no error until the player has touched the control', async () => {
    const { name } = await setup();
    const input = await name.getInput();
    await input.focus();
    expect(await name.getError()).toBeNull();

    await input.blur();
    expect(await name.getError()).toBe('Name your character.');
    expect(await name.getHint()).toBeNull();
    expect(await input.isInvalid()).toBeTrue();
    expect(await input.getDescription()).toBe('✕Name your character.');
  });

  it('says what is wrong as the value changes, and clears when it is fixed', async () => {
    const { name } = await setup();
    const input = await name.getInput();

    await input.setValue('Al');
    expect(await name.getError()).toBe('At least 3 characters.');

    await input.setValue('Aldric');
    expect(await name.getError()).toBeNull();
    expect(await name.getHint()).toBe('3 to 16 letters');
    expect(await input.isInvalid()).toBeFalse();
  });

  it('an error of your own shows at once; a textarea is a field too', async () => {
    const { fixture, note } = await setup();
    fixture.componentInstance.serverError.set(
      'Couldn’t save. Your change was undone.',
    );
    fixture.detectChanges();

    expect(await note.getError()).toBe(
      'Couldn’t save. Your change was undone.',
    );
    const area = await note.getInput();
    expect(await area.isInvalid()).toBeTrue();
    expect(await area.isRequired()).toBeFalse();
  });

  it('turns a validator’s error into words', () => {
    expect(lgFieldMessage({ maxlength: { requiredLength: 16 } })).toBe(
      'At most 16 characters.',
    );
    expect(lgFieldMessage({ email: true })).toBe('Enter an email address.');
    expect(lgFieldMessage({ taken: 'That name is taken.' })).toBe(
      'That name is taken.',
    );
    expect(lgFieldMessage(null)).toBeNull();
  });
});
