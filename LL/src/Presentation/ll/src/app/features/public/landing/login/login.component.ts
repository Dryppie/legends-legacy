import { Component } from '@angular/core';
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
import { ButtonComponent } from '../../../../shared/components/custom-components/buttons/button/button.component';
import { environment } from '../../../../../environments/environment';
import { GoogleAuthService } from '../../../../core/services/api/auth/google-auth.service';

@Component({
    selector: 'app-login',
    imports: [
        RouterLink,
        ReactiveFormsModule,
        NgIf,
        ButtonComponent,
        MatIconModule,
        MatFormFieldModule,
        MatInputModule,
        NgClass,
    ],
    templateUrl: './login.component.html'
})
export class LoginComponent {
  document: any;
  constructor(
    private authService: AuthService,
    private googleService: GoogleAuthService,
    private router: Router,
  ) {}
  loginError: boolean = false;
  validatorError: boolean | undefined = false;

  loginForm = new FormGroup({
    email: new FormControl(environment.isLocal ? 'admin@hotmail.com' : '', [
      Validators.required,
      emailValidator(),
    ]),
    password: new FormControl(environment.isLocal ? 'Password123!' : '', [
      Validators.required,
      passwordValidator(),
      Validators.minLength(8),
    ]),
  });

  onGoogleClick() {
    this.googleService.prompt();
  }

  login() {
    const email = this.loginForm.value.email;
    const password = this.loginForm.value.password;
    if (typeof email === 'string' && typeof password === 'string') {
      this.authService.login(email, password).subscribe({
        next: () => {
          this.router.navigateByUrl('/game');
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
  validateEmail() {
    return this.validateField('email');
  }

  validatePassword() {
    return this.validateField('password');
  }

  private validateField(field: string) {
    const control = this.loginForm.get(field);
    return control?.invalid && (control.dirty || control.touched);
  }

  loginAsGuest() {
    this.authService.loginAsGuest();
  }
}
