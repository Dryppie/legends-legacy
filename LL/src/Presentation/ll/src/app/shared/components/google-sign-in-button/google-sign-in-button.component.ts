import { NgIf } from '@angular/common';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  ViewChild,
} from '@angular/core';
import { GoogleAuthService } from '../../../core/services/api/auth/google-auth.service';

type GoogleAction = 'sign-in' | 'bind';

@Component({
  selector: 'app-google-sign-in-button',
  imports: [NgIf],
  templateUrl: './google-sign-in-button.component.html',
})
export class GoogleSignInButtonComponent implements AfterViewInit {
  @Input() action: GoogleAction = 'sign-in';

  @ViewChild('buttonHost', { static: true })
  private buttonHost!: ElementRef<HTMLDivElement>;

  isLoading = true;
  loadFailed = false;

  constructor(private readonly googleAuth: GoogleAuthService) {}

  ngAfterViewInit(): void {
    this.render();
  }

  retry(): void {
    this.render();
  }

  get ariaLabel(): string {
    return this.action === 'bind'
      ? 'Bind Google account'
      : 'Continue with Google';
  }

  private render(): void {
    this.isLoading = true;
    this.loadFailed = false;

    void this.googleAuth
      .renderButton(this.buttonHost.nativeElement)
      .then(() => {
        this.isLoading = false;
      })
      .catch((error: unknown) => {
        console.error(`${this.ariaLabel} initialization failed.`, error);
        this.isLoading = false;
        this.loadFailed = true;
      });
  }
}
