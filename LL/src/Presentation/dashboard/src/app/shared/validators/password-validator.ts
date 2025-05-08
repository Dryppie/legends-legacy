import { AbstractControl, ValidatorFn } from '@angular/forms';

export function passwordValidator(): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const password = control.value;

    // Regular expression for allowed characters
    const validCharactersRegex = /^.+$/;

    const isValid =
      password &&
      typeof password === 'string' &&
      validCharactersRegex.test(password);

    return isValid ? null : { invalidCustomPassword: { value: password } };
  };
}
