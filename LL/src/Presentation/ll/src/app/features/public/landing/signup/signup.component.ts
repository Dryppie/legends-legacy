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
import { AuthService } from '../../../../core/services/auth/auth.service';
import { emailValidator } from '../../../../shared/validators/email-validator';
import { passwordValidator } from '../../../../shared/validators/password-validator';
import { passwordMatchValidator } from '../../../../shared/validators/password-match-validator';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
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
  styleUrl: './signup.component.css',
})
export class SignupComponent {
  registerForm = new FormGroup(
    {
      username: new FormControl(environment.isLocal ? 'Dryp' : ''),
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
  constructor(
    private authService: AuthService,
    private router: Router,
  ) {}
  loginError: boolean = false;

  register() {
    const username = this.registerForm.value.username;
    const email = this.registerForm.value.email;
    const password = this.registerForm.value.password;
    const confirmPassword = this.registerForm.value.confirmPassword;
    if (
      typeof username === 'string' &&
      typeof email === 'string' &&
      typeof password === 'string'
    ) {
      this.authService.register(username, email, password).subscribe({
        next: () => {
          this.router.navigateByUrl('/login');
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
    const control = this.registerForm.get(field);
    return control?.invalid && (control.dirty || control.touched);
  }
}
