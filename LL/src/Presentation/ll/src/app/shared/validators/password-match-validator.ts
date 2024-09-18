import { AbstractControl, FormGroup, ValidatorFn } from '@angular/forms';

export function passwordMatchValidator(): ValidatorFn {
  return (formGroup: AbstractControl): { [key: string]: any } | null => {
    const password = formGroup.get('password')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    return password === confirmPassword
      ? null
      : {
          invalidCustomEmail: {
            password: password,
            confirmPassword: confirmPassword,
          },
        };
  };
}
