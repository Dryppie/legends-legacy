import { AbstractControl, ValidatorFn } from '@angular/forms';

export function emailValidator(): ValidatorFn {
  return (control: AbstractControl): { [key: string]: any } | null => {
    const email = control.value;
    const atIndex = email ? email.indexOf('@') : -1;
    const dotIndex = email ? email.lastIndexOf('.') : -1;

    const isValid =
      email &&
      typeof email === 'string' &&
      atIndex > -1 && // '@' exists
      dotIndex > atIndex + 1 && // '.' is after '@' and not immediately after
      dotIndex < email.length - 1 && // at least one character after '.'
      !email.includes(' '); // no spaces

    return isValid ? null : { invalidCustomEmail: { value: email } };
  };
}
