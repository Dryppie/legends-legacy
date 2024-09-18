import { Component } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NgIf } from '@angular/common';
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
  ],
  templateUrl: './signup.component.html',
  styleUrl: './signup.component.css',
})
export class SignupComponent {
  registerForm = new FormGroup(
    {
      username: new FormControl('Dryp'),
      email: new FormControl('Dryp@hotmail.com', [
        Validators.required,
        emailValidator(),
      ]),
      password: new FormControl('drypping', [
        Validators.required,
        passwordValidator(),
        Validators.minLength(8),
      ]),
      confirmPassword: new FormControl('drypping'),
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
}
