import { Component, Input } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgClass, NgIf } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AuthService } from '../../../../core/services/api/auth/auth.service';
import { emailValidator } from '../../../../shared/validators/email-validator';
import { passwordValidator } from '../../../../shared/validators/password-validator';
import { passwordMatchValidator } from '../../../../shared/validators/password-match-validator';
import { ButtonComponent } from '../../../../shared/components/custom-components/buttons/button/button.component';
import { environment } from '../../../../../environments/environment';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [
    RouterLink,
    ReactiveFormsModule,
    NgIf,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    NgClass,
    ButtonComponent,
  ],
  templateUrl: './signup.component.html',
})
export class SignupComponent {
  @Input() convertAccount: boolean = false;
  @Input() prefilledUsername: string | null = null;
  @Input() disableLoginLink: boolean = false;
  @Input() headerText1: string = 'Join the adventure and';
  @Input() headerText2: string = 'create your legend!';
  @Input() usernameLabel: string = 'Character name';
  @Input() usernameHelp: string | null = null;

  registerForm = new FormGroup(
    {
      username: new FormControl(
        environment.isLocal ? 'Dryp' : '',
        Validators.maxLength(26),
      ),
      email: new FormControl(environment.isLocal ? 'Dryp@hotmail.com' : '', [
        Validators.required,
        emailValidator(),
      ]),
      password: new FormControl(environment.isLocal ? 'Drypping' : '', [
        Validators.required,
        passwordValidator(),
        Validators.minLength(8),
      ]),
      confirmPassword: new FormControl(environment.isLocal ? 'Drypping' : ''),
    },
    { validators: passwordMatchValidator() },
  );

  ngOnInit() {
    if (this.prefilledUsername) {
      this.registerForm.patchValue({ username: this.prefilledUsername });
    }
  }

  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}
  loginError: boolean = false;

  submitForm() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched(); // <-- This will show validation errors nicely
      return; // Stop the form from submitting if it's invalid
    }

    if (this.convertAccount) {
      this.convertAcc();
    } else {
      this.register();
    }
  }

  register() {
    const characterName = this.registerForm.value.username;
    const email = this.registerForm.value.email;
    const password = this.registerForm.value.password;
    const confirmPassword = this.registerForm.value.confirmPassword;
    if (
      typeof characterName === 'string' &&
      typeof email === 'string' &&
      typeof password === 'string'
    ) {
      this.authService.register(characterName, email, password).subscribe({
        next: () => {
          this.router.navigateByUrl('/login');
        },
        error: () => {
          this.loginError = true;
        },
      });
    }
  }

  convertAcc() {
    const characterName = this.registerForm.value.username;
    const email = this.registerForm.value.email;
    const password = this.registerForm.value.password;
    const confirmPassword = this.registerForm.value.confirmPassword;
    if (
      typeof characterName === 'string' &&
      typeof email === 'string' &&
      typeof password === 'string'
    ) {
      this.authService.convertGuestToUser(characterName, email, password).subscribe({
        next: () => {
          // window.location.reload();
        },
        error: () => {
          this.loginError = true;
        },
      });
    }
  }

  resetLoginError() {
    this.loginError = false;
  }

  validateUsername() {
    return this.validateField('username');
  }

  validateEmail() {
    return this.validateField('email');
  }

  validatePassword() {
    return this.validateField('password');
  }

  validateConfirmPassword() {
    return this.registerForm.hasError('passwordMismatch');
  }

  private validateField(field: string) {
    const control = this.registerForm.get(field);
    return control?.invalid && (control.dirty || control.touched);
  }
}
